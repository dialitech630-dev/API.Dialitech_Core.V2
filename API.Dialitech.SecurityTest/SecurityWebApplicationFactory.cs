using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace API.Dialitech.SecurityTest;

public class SecurityWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("MongoDbSettings:ConnectionString", "mongodb://localhost:27017");
        builder.UseSetting("MongoDbSettings:DatabaseName", "DialitechDB_Test_Security");
        builder.UseSetting("JwtSettings:SecretKey", "test-key-32-characters-minimum-replace-me");
        builder.UseSetting("JwtSettings:Issuer", "API.Dialitech.Test");
        builder.UseSetting("JwtSettings:Audience", "API.Dialitech.Test");
        builder.UseSetting("JwtSettings:ExpirationInMinutes", "60");
    }
}
