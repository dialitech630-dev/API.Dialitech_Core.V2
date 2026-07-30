using API.Dialitech.Domain.Entities;
using API.Dialitech.Domain.Enums;
using API.Dialitech.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace API.Dialitech.Infrastructure.Data;

public static class MongoDataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<MongoDbContext>>();

        if (await context.Caregivers.Find(c => c.Email == "demo@test.com").AnyAsync())
        {
            logger.LogInformation("Seed data already exists, skipping.");
            return;
        }

        var caregiver = new Caregiver
        {
            Name = "Demo",
            Lastname = "User",
            Phone = "+525500000000",
            ImageUrl = "",
            Email = "demo@test.com",
            PasswordHash = hasher.Hash("Demo123!"),
            Plan = Plan.Premium,
            CreatedAt = DateTime.UtcNow
        };
        await context.Caregivers.InsertOneAsync(caregiver);

        var patient = new Patient
        {
            CaregiverId = caregiver.Id,
            Name = "Paciente Demo",
            Age = 35,
            Gender = "Masculino",
            Notes = "Paciente de prueba",
            Code = "DEMO001",
            CodeExpiresAt = DateTime.UtcNow.AddDays(30),
            DeviceSerialNumber = "SN-DEMO-001",
            LastHeartRate = 72.0,
            LastOxygen = 97.5,
            LastActivity = 45.0,
            LastReadingAt = DateTime.UtcNow.AddMinutes(-2),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await context.Patients.InsertOneAsync(patient);

        var device = new Device
        {
            PatientId = patient.Id,
            SerialNumber = "SN-DEMO-001",
            IsActive = true,
            RegisteredAt = DateTime.UtcNow
        };
        await context.Devices.InsertOneAsync(device);

        logger.LogInformation("Seed data created: caregiver={Email}, patient={Code}", caregiver.Email, patient.Code);
    }
}
