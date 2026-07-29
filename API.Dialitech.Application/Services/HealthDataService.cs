using API.Dialitech.Application.Common.Exceptions;
using API.Dialitech.Application.DTOs;
using API.Dialitech.Application.Interfaces;
using API.Dialitech.Domain.Entities;
using API.Dialitech.Domain.Interfaces;

namespace API.Dialitech.Application.Services;

public class HealthDataService : IHealthDataService
{
    private readonly IPatientRepository _patientRepo;
    private readonly IAlertRepository _alertRepo;

    public HealthDataService(IPatientRepository patientRepo, IAlertRepository alertRepo)
    {
        _patientRepo = patientRepo;
        _alertRepo = alertRepo;
    }

    public async Task<BatchResponse> ProcessBatchAsync(BatchRequest request)
    {
        if (request.Data is null || request.Data.Count == 0)
            throw new ValidationException("Data", "No data points provided.");

        var patient = await _patientRepo.GetByCodeAsync(request.PatientCode)
            ?? throw new NotFoundException("Patient", request.PatientCode);

        var alerts = new List<Alert>();
        BatchDataPoint? lastPoint = null;

        foreach (var point in request.Data)
        {
            lastPoint = point;

            if (point.HeartRate < 50)
                alerts.Add(CreateAlert(patient, "HeartRateLow",
                    $"Heart rate too low: {point.HeartRate} bpm", 2));

            if (point.HeartRate > 120)
                alerts.Add(CreateAlert(patient, "HeartRateHigh",
                    $"Heart rate too high: {point.HeartRate} bpm", 2));

            if (point.Oxygen < 90)
                alerts.Add(CreateAlert(patient, "OxygenLow",
                    $"Oxygen level low: {point.Oxygen}%", 2));
        }

        if (alerts.Count > 0)
        {
            await _alertRepo.CreateAsync(alerts[0]);
        }

        if (lastPoint is not null)
        {
            patient.LastHeartRate = lastPoint.HeartRate;
            patient.LastOxygen = lastPoint.Oxygen;
            patient.LastActivity = lastPoint.Activity;
            patient.LastReadingAt = lastPoint.Timestamp;
            patient.UpdatedAt = DateTime.UtcNow;
            await _patientRepo.UpdateAsync(patient);
        }

        return new BatchResponse
        {
            Status = "processed",
            AlertsTriggered = alerts.Count
        };
    }

    private static Alert CreateAlert(Patient patient, string type, string message, int severity)
    {
        return new Alert
        {
            PatientId = patient.Id,
            CaregiverId = patient.CaregiverId,
            Type = type,
            Message = message,
            Severity = severity,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
    }
}
