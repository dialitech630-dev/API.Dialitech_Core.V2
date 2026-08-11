using API.Dialitech.Application.Common.Exceptions;
using API.Dialitech.Application.DTOs;
using API.Dialitech.Application.Interfaces;
using API.Dialitech.Domain.Entities;
using API.Dialitech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace API.Dialitech.Application.Services;

public class HealthDataService : IHealthDataService
{
    private readonly IPatientRepository _patientRepo;
    private readonly IAlertRepository _alertRepo;
    private readonly IReadingRepository _readingRepo;
    private readonly INotificationService _notificationService;
    private readonly ILogger<HealthDataService> _logger;

    public HealthDataService(
        IPatientRepository patientRepo,
        IAlertRepository alertRepo,
        IReadingRepository readingRepo,
        INotificationService notificationService,
        ILogger<HealthDataService> logger)
    {
        _patientRepo = patientRepo;
        _alertRepo = alertRepo;
        _readingRepo = readingRepo;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<BatchResponse> ProcessBatchAsync(BatchRequest request)
    {
        if (request.Data is null || request.Data.Count == 0)
            throw new ValidationException("Data", "No data points provided.");

        var patient = await _patientRepo.GetByCodeAsync(request.PatientCode)
            ?? throw new NotFoundException("Patient", request.PatientCode);

        var alerts = new List<Alert>();
        BatchDataPoint? lastPoint = null;
        Alert? mostCriticalAlert = null;

        foreach (var point in request.Data)
        {
            lastPoint = point;

            var alert = EvaluateAlert(patient, point);
            if (alert is not null)
            {
                alerts.Add(alert);
                if (mostCriticalAlert is null || alert.Severity > mostCriticalAlert.Severity)
                    mostCriticalAlert = alert;
            }
        }

        if (alerts.Count > 0)
        {
            await _alertRepo.CreateAsync(alerts[0]);

            if (!string.IsNullOrWhiteSpace(patient.DeviceToken) && mostCriticalAlert is not null)
            {
                try
                {
                    await _notificationService.SendHealthAlertAsync(
                        patient.DeviceToken,
                        "Alerta de salud",
                        mostCriticalAlert.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to send push notification for patient {PatientId}", patient.Id);
                }
            }
        }

        var readings = request.Data.Select(point => new Reading
        {
            PatientId = patient.Id,
            CaregiverId = patient.CaregiverId,
            HeartRate = point.HeartRate,
            Oxygen = point.Oxygen,
            Activity = point.Activity,
            Timestamp = point.Timestamp,
            CreatedAt = DateTime.UtcNow
        });
        await _readingRepo.AddManyAsync(readings);

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

    public async Task<PatientInfoResponse> GetPatientInfoAsync(string patientCode)
    {
        var patient = await _patientRepo.GetByCodeAsync(patientCode)
            ?? throw new NotFoundException("Patient", patientCode);

        return new PatientInfoResponse
        {
            PatientCode = patient.Code ?? patientCode,
            Name = patient.Name,
            DeviceSerialNumber = patient.DeviceSerialNumber,
            HasDeviceToken = !string.IsNullOrWhiteSpace(patient.DeviceToken),
            LastHeartRate = patient.LastHeartRate,
            LastOxygen = patient.LastOxygen,
            LastActivity = patient.LastActivity,
            LastReadingAt = patient.LastReadingAt
        };
    }

    public async Task RegisterDeviceTokenAsync(string patientCode, string deviceToken)
    {
        if (string.IsNullOrWhiteSpace(deviceToken))
            throw new ValidationException("DeviceToken", "Device token is required.");

        var patient = await _patientRepo.GetByCodeAsync(patientCode)
            ?? throw new NotFoundException("Patient", patientCode);

        patient.DeviceToken = deviceToken;
        patient.DeviceTokenUpdatedAt = DateTime.UtcNow;
        await _patientRepo.UpdateAsync(patient);
    }

    private static Alert? EvaluateAlert(Patient patient, BatchDataPoint point)
    {
        if (point.HeartRate < 50)
            return CreateAlert(patient, "HeartRateLow",
                $"Heart rate too low: {point.HeartRate} bpm", 2);

        if (point.HeartRate > 120)
            return CreateAlert(patient, "HeartRateHigh",
                $"Heart rate too high: {point.HeartRate} bpm", 2);

        if (point.Oxygen < 90)
            return CreateAlert(patient, "OxygenLow",
                $"Oxygen level low: {point.Oxygen}%", 2);

        return null;
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
