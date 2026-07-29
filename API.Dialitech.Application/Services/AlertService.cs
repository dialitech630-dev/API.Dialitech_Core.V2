using API.Dialitech.Application.Common.Exceptions;
using API.Dialitech.Application.DTOs;
using API.Dialitech.Application.Interfaces;
using API.Dialitech.Domain.Interfaces;

namespace API.Dialitech.Application.Services;

public class AlertService : IAlertService
{
    private readonly IAlertRepository _alertRepo;
    private readonly IPatientRepository _patientRepo;

    public AlertService(IAlertRepository alertRepo, IPatientRepository patientRepo)
    {
        _alertRepo = alertRepo;
        _patientRepo = patientRepo;
    }

    public async Task<IEnumerable<AlertDto>> GetByCaregiverAsync(string caregiverId)
    {
        var patients = (await _patientRepo.GetByCaregiverIdAsync(caregiverId))
            .ToDictionary(p => p.Id);

        var alerts = await _alertRepo.GetByCaregiverIdAsync(caregiverId);

        return alerts.Select(a => new AlertDto
        {
            Id = a.Id,
            PatientId = a.PatientId,
            PatientName = patients.TryGetValue(a.PatientId, out var p) ? p.Name : "Unknown",
            Type = a.Type,
            Message = a.Message,
            Severity = a.Severity,
            IsRead = a.IsRead,
            CreatedAt = a.CreatedAt
        });
    }

    public async Task<IEnumerable<AlertDto>> GetByPatientAsync(string patientId, string caregiverId)
    {
        var patient = await _patientRepo.GetByIdAsync(patientId);
        if (patient is null || patient.CaregiverId != caregiverId)
            return [];

        var alerts = await _alertRepo.GetByPatientIdAsync(patientId);

        return alerts.Select(a => new AlertDto
        {
            Id = a.Id,
            PatientId = a.PatientId,
            PatientName = patient.Name,
            Type = a.Type,
            Message = a.Message,
            Severity = a.Severity,
            IsRead = a.IsRead,
            CreatedAt = a.CreatedAt
        });
    }

    public async Task DeleteAsync(string alertId, string caregiverId)
    {
        // Verify alert belongs to caregiver through patient chain
        var alert = await _alertRepo.GetByPatientIdAsync(alertId);
        // For simplicity, fetch alert by getting all caregiver alerts and matching
        var caregiverAlerts = await _alertRepo.GetByCaregiverIdAsync(caregiverId);
        var match = caregiverAlerts.FirstOrDefault(a => a.Id == alertId);

        if (match is null)
            throw new NotFoundException("Alert", alertId);

        await _alertRepo.DeleteAsync(alertId);
    }
}
