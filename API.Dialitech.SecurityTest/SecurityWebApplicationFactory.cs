using API.Dialitech.Domain.Interfaces;
using API.Dialitech.IntegrationTest;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

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

        builder.ConfigureServices(services =>
        {
            var descriptorsToRemove = services.Where(d =>
                d.ServiceType == typeof(ICaregiverRepository) ||
                d.ServiceType == typeof(IPatientRepository) ||
                d.ServiceType == typeof(IDeviceRepository) ||
                d.ServiceType == typeof(IAlertRepository)).ToList();

            foreach (var descriptor in descriptorsToRemove)
                services.Remove(descriptor);

            services.AddSingleton<ICaregiverRepository, InMemoryCaregiverRepository>();
            services.AddSingleton<IPatientRepository, InMemoryPatientRepository>();
            services.AddSingleton<IDeviceRepository, InMemoryDeviceRepository>();
            services.AddSingleton<IAlertRepository, InMemoryAlertRepository>();
        });
    }
}
