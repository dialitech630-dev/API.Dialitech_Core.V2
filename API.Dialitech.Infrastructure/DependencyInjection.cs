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

        return services;
    }
}
