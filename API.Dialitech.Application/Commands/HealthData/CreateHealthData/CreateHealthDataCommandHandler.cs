using API.Dialitech.Application.DTOs;
using API.Dialitech.Domain.Entities;
using API.Dialitech.Domain.Interfaces;
using MediatR;

namespace API.Dialitech.Application.Commands.HealthData.CreateHealthData;

public class CreateHealthDataCommandHandler : IRequestHandler<CreateHealthDataCommand, HealthDataDto>
{
    private readonly IHealthRecordRepository _repository;
    private readonly IAlertRepository _alertRepository;

    public CreateHealthDataCommandHandler(IHealthRecordRepository repository, IAlertRepository alertRepository)
    {
        _repository = repository;
        _alertRepository = alertRepository;
    }

    public async Task<HealthDataDto> Handle(CreateHealthDataCommand request, CancellationToken cancellationToken)
    {
        var record = new HealthRecord
        {
            UserId = request.UserId,
            HeartRate = request.HeartRate,
            SpO2 = request.SpO2,
            ActivityLevel = request.ActivityLevel,
            Timestamp = request.Timestamp
        };

        await _repository.CreateAsync(record);

        await GenerateAlerts(record);

        return new HealthDataDto
        {
            Id = record.Id,
            UserId = record.UserId,
            HeartRate = record.HeartRate,
            SpO2 = record.SpO2,
            ActivityLevel = record.ActivityLevel,
            Timestamp = record.Timestamp
        };
    }

    private async Task GenerateAlerts(HealthRecord record)
    {
        var alerts = new List<Alert>();

        if (record.HeartRate > 120)
        {
            alerts.Add(new Alert
            {
                UserId = record.UserId,
                Type = "warning",
                Message = "Frecuencia cardíaca elevada",
                Severity = 2,
                Timestamp = DateTime.UtcNow,
                IsRead = false
            });
        }

        if (record.HeartRate < 50)
        {
            alerts.Add(new Alert
            {
                UserId = record.UserId,
                Type = "warning",
                Message = "Frecuencia cardíaca baja",
                Severity = 2,
                Timestamp = DateTime.UtcNow,
                IsRead = false
            });
        }

        if (record.SpO2 < 90)
        {
            alerts.Add(new Alert
            {
                UserId = record.UserId,
                Type = "critical",
                Message = "Saturación de oxígeno crítica",
                Severity = 3,
                Timestamp = DateTime.UtcNow,
                IsRead = false
            });
        }

        if (record.SpO2 < 95)
        {
            alerts.Add(new Alert
            {
                UserId = record.UserId,
                Type = "warning",
                Message = "Saturación de oxígeno baja",
                Severity = 2,
                Timestamp = DateTime.UtcNow,
                IsRead = false
            });
        }

        foreach (var alert in alerts)
        {
            await _alertRepository.CreateAsync(alert);
        }
    }
}
