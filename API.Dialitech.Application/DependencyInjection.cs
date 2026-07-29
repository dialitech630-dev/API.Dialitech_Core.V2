using System.Reflection;
using API.Dialitech.Application.Interfaces;
using API.Dialitech.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace API.Dialitech.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<IHealthDataService, HealthDataService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAlertService, AlertService>();

        return services;
    }
}
