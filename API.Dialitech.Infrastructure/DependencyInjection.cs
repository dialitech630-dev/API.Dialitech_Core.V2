using API.Dialitech.Application.Interfaces;
using API.Dialitech.Domain.Interfaces;
using API.Dialitech.Infrastructure.Data;
using API.Dialitech.Infrastructure.Data.Repositories;
using API.Dialitech.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace API.Dialitech.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MongoDbSettings>(configuration.GetSection("MongoDbSettings"));
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

        services.AddSingleton<MongoDbContext>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ICaregiverRepository, CaregiverRepository>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IDeviceRepository, DeviceRepository>();
        services.AddScoped<IAlertRepository, AlertRepository>();
        services.AddScoped<IReadingRepository, ReadingRepository>();

        var firebaseCredentials = configuration["FIREBASE_ADMIN_CREDENTIALS"];
        if (string.IsNullOrWhiteSpace(firebaseCredentials))
        {
            var credsFile = Path.Combine(AppContext.BaseDirectory, "firebase-admin.json");
            if (File.Exists(credsFile))
                firebaseCredentials = File.ReadAllText(credsFile);
        }

        if (!string.IsNullOrWhiteSpace(firebaseCredentials))
        {
            FirebaseNotificationService.Initialize(firebaseCredentials);
            services.AddSingleton<INotificationService, FirebaseNotificationService>();
        }
        else
        {
            services.AddSingleton<INotificationService, NoopNotificationService>();
        }

        return services;
    }
}
