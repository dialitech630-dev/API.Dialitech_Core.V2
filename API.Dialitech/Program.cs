using System.Text;
using API.Dialitech.Application;
using API.Dialitech.HealthChecks;
using API.Dialitech.Infrastructure;
using API.Dialitech.Infrastructure.Data;
using API.Dialitech.Infrastructure.Services;
using API.Dialitech.Middleware;
using API.Dialitech.OpenApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

static int IntOrDefault(string? value, int fallback) =>
    int.TryParse(value, out var parsed) ? parsed : fallback;

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JwtSettings not configured");

if (string.IsNullOrEmpty(jwtSettings.SecretKey))
    throw new InvalidOperationException("JwtSettings:SecretKey is not configured.");

var mongoConnectionString = builder.Configuration["MongoDbSettings:ConnectionString"];
if (string.IsNullOrEmpty(mongoConnectionString))
    throw new InvalidOperationException("MongoDbSettings:ConnectionString is not configured.");

builder.Services.AddControllers();

// ML Service HttpClient
builder.Services.AddHttpClient("MlService", client =>
{
    var mlBaseUrl = builder.Configuration["MlService:BaseUrl"] ?? "http://localhost:8000";
    var mlApiKey = builder.Configuration["MlService:ApiKey"] ?? "test-key";
    client.BaseAddress = new Uri(mlBaseUrl);
    client.DefaultRequestHeaders.Add("X-API-Key", mlApiKey);
    client.Timeout = TimeSpan.FromSeconds(30);
});
var openApiServerUrl = builder.Configuration["OpenApi:ServerUrl"]
    ?? (builder.Environment.IsDevelopment() ? null : "https://api-dialitech-core-v2.onrender.com");

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        if (openApiServerUrl is not null)
        {
            document.Servers =
            [
                new OpenApiServer
                {
                    Url = openApiServerUrl,
                    Description = builder.Environment.IsDevelopment() ? "Development" : "Production"
                }
            ];
        }
        return Task.CompletedTask;
    });
});
builder.Services.AddSingleton<IOpenApiDocumentTransformer, BearerSecuritySchemeTransformer>();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationServices();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
        };
    });

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", cfg =>
    {
        cfg.PermitLimit = 100;
        cfg.Window = TimeSpan.FromMinutes(1);
        cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        cfg.QueueLimit = 5;
    });

    options.AddFixedWindowLimiter("login", cfg =>
    {
        cfg.PermitLimit = 5;
        cfg.Window = TimeSpan.FromMinutes(1);
        cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        cfg.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("batch", cfg =>
    {
        cfg.PermitLimit = 30;
        cfg.Window = TimeSpan.FromMinutes(1);
        cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        cfg.QueueLimit = 10;
    });

    options.AddFixedWindowLimiter("sensitive", cfg =>
    {
        cfg.PermitLimit = IntOrDefault(builder.Configuration["RateLimiting:SensitivePermitLimit"], 30);
        cfg.Window = TimeSpan.FromMinutes(1);
        cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        cfg.QueueLimit = 2;
    });

    options.AddFixedWindowLimiter("register", cfg =>
    {
        cfg.PermitLimit = IntOrDefault(builder.Configuration["RateLimiting:RegisterPermitLimit"], 15);
        cfg.Window = TimeSpan.FromHours(1);
        cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        cfg.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("auth-restore", cfg =>
    {
        cfg.PermitLimit = IntOrDefault(builder.Configuration["RateLimiting:AuthRestorePermitLimit"], 5);
        cfg.Window = TimeSpan.FromMinutes(1);
        cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        cfg.QueueLimit = 0;
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var corsOrigins = builder.Configuration["Cors:Origins"] ?? "http://localhost:3000";
var allowedOrigins = corsOrigins
    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecific", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddHealthChecks()
    .AddCheck<MongoHealthCheck>("mongodb");

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
});

var app = builder.Build();

if (string.IsNullOrWhiteSpace(app.Configuration["MlService:ApiKey"]) ||
    string.Equals(app.Configuration["MlService:ApiKey"], "test-key", StringComparison.Ordinal))
{
    app.Logger.LogWarning(
        "MlService:ApiKey no está configurado; se usará la clave por defecto 'test-key'. Configúrala en producción.");
}

if (app.Environment.IsProduction() &&
    app.Configuration.GetValue<string>("MlService:BaseUrl")?.Contains("localhost", StringComparison.OrdinalIgnoreCase) == true)
{
    app.Logger.LogWarning(
        "MlService:BaseUrl apunta a localhost en producción; el análisis ML fallará silenciosamente.");
}

if (!app.Environment.IsDevelopment())
{
    await MongoDataSeeder.SeedAsync(app.Services);
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionHandler>();

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("X-XSS-Protection", "0");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000");
    context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");

    if (app.Environment.IsDevelopment())
    {
        context.Response.Headers.Append("Content-Security-Policy",
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "connect-src 'self' ws: http://localhost:*; " +
            "img-src 'self' data:; " +
            "font-src 'self' data:");
    }
    else
    {
        context.Response.Headers.Append("Content-Security-Policy",
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data:; " +
            "font-src 'self' data:");
    }

    await next();
});

var openApiAccessToken = builder.Configuration["OpenApi:AccessToken"];
if (!string.IsNullOrWhiteSpace(openApiAccessToken))
{
    app.UseWhen(
        ctx => ctx.Request.Path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase)
            || ctx.Request.Path.StartsWithSegments("/scalar", StringComparison.OrdinalIgnoreCase),
        branch => branch.Use(async (ctx, next) =>
        {
            var provided = ctx.Request.Headers["X-API-Access"].ToString();
            if (string.IsNullOrWhiteSpace(provided))
                provided = ctx.Request.Query["x-api-access"].ToString();

            if (!string.Equals(provided, openApiAccessToken, StringComparison.Ordinal))
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            await next();
        }));
}

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("OpenApi:Enabled"))
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("API.Dialitech - Health Monitoring")
               .WithTheme(ScalarTheme.Purple)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

app.UseSerilogRequestLogging();

app.UseCors("AllowSpecific");

if (!app.Environment.IsDevelopment() || app.Configuration["ASPNETCORE_HTTPS_PORTS"] is not null)
{
    app.UseHttpsRedirection();
}

app.UseRateLimiter();

app.MapHealthChecks("/health");

app.UseAuthentication();

app.UseMiddleware<AuditLogMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();
