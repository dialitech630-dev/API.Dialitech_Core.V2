using System.Net.Http.Json;
using System.Text.Json;
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
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HealthDataService> _logger;

    public HealthDataService(
        IPatientRepository patientRepo,
        IAlertRepository alertRepo,
        IReadingRepository readingRepo,
        INotificationService notificationService,
        IHttpClientFactory httpClientFactory,
        ILogger<HealthDataService> logger)
    {
        _patientRepo = patientRepo;
        _alertRepo = alertRepo;
        _readingRepo = readingRepo;
        _notificationService = notificationService;
        _httpClientFactory = httpClientFactory;
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

        // ── ML Service Integration ──────────────────────────────────
        var mlAlertsCount = await AnalyzeWithMlServiceAsync(patient, request.Data, alerts);
        // ─────────────────────────────────────────────────────────────

        return new BatchResponse
        {
            Status = "processed",
            AlertsTriggered = alerts.Count + mlAlertsCount
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

    private async Task<int> AnalyzeWithMlServiceAsync(
        Patient patient,
        List<BatchDataPoint> dataPoints,
        List<Alert> ruleBasedAlerts)
    {
        var mlAlertsCreated = 0;

        try
        {
            var httpClient = _httpClientFactory.CreateClient("MlService");

            var mlRequest = new MlAnalysisRequest
            {
                PatientId = patient.Id,
                WindowSize = 12,
                Readings = dataPoints.Select(dp => new MlReadingDto
                {
                    HeartRate = dp.HeartRate,
                    Oxygen = dp.Oxygen,
                    Activity = dp.Activity,
                    Timestamp = dp.Timestamp
                }).ToList()
            };

            var response = await httpClient.PostAsJsonAsync("/api/v1/analyze", mlRequest);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "ML Service returned {StatusCode} for patient {PatientCode}",
                    response.StatusCode, patient.Code);
                return 0;
            }

            var mlResult = await response.Content.ReadFromJsonAsync<MlAnalysisResponse>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (mlResult is null) return 0;

            // ── HIGH RISK ALERT ─────────────────────────────────────
            if (string.Equals(mlResult.RiskPrediction.RiskLevel, "HIGH", StringComparison.OrdinalIgnoreCase))
            {
                var alert = CreateAlert(patient, "ML-HighRisk",
                    $"ML Risk: {mlResult.RiskPrediction.RiskScore:P0} — {mlResult.RiskPrediction.Recommendation}",
                    severity: 3);
                await _alertRepo.CreateAsync(alert);
                mlAlertsCreated++;
            }

            // ── MEDIUM RISK (only if no rule-based alert already) ────
            if (string.Equals(mlResult.RiskPrediction.RiskLevel, "MEDIUM", StringComparison.OrdinalIgnoreCase)
                && ruleBasedAlerts.Count == 0)
            {
                var alert = CreateAlert(patient, "ML-MediumRisk",
                    $"ML Risk: {mlResult.RiskPrediction.RiskScore:P0} — {mlResult.RiskPrediction.Recommendation}",
                    severity: 2);
                await _alertRepo.CreateAsync(alert);
                mlAlertsCreated++;
            }

            // ── ANOMALY ALERT ───────────────────────────────────────
            if (mlResult.AnomalyDetection.AnomalyDetected)
            {
                var metrics = string.Join(", ", mlResult.AnomalyDetection.AffectedMetrics);
                var alert = CreateAlert(patient, "ML-Anomaly",
                    $"Anomaly (score {mlResult.AnomalyDetection.AnomalyScore:F2}): {metrics}",
                    severity: 2);
                await _alertRepo.CreateAsync(alert);
                mlAlertsCreated++;
            }

            _logger.LogInformation(
                "ML analysis complete for {PatientCode}: risk={RiskLevel}, anomaly={AnomalyDetected}",
                patient.Code,
                mlResult.RiskPrediction.RiskLevel,
                mlResult.AnomalyDetection.AnomalyDetected);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex,
                "ML Service analysis failed for patient {PatientCode} — continuing with rule-based alerts",
                patient.Code);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex,
                "ML Service analysis failed for patient {PatientCode} — continuing with rule-based alerts",
                patient.Code);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex,
                "ML Service analysis failed for patient {PatientCode} — continuing with rule-based alerts",
                patient.Code);
        }
        catch (NotSupportedException ex)
        {
            _logger.LogWarning(ex,
                "ML Service analysis failed for patient {PatientCode} — continuing with rule-based alerts",
                patient.Code);
        }

        return mlAlertsCreated;
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
