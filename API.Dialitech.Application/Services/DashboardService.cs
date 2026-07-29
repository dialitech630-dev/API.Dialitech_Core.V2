using API.Dialitech.Application.DTOs;
using API.Dialitech.Application.Interfaces;
using API.Dialitech.Domain.Interfaces;

namespace API.Dialitech.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IPatientRepository _patientRepo;
    private readonly IAlertRepository _alertRepo;

    public DashboardService(IPatientRepository patientRepo, IAlertRepository alertRepo)
    {
        _patientRepo = patientRepo;
        _alertRepo = alertRepo;
    }

    public async Task<DashboardSummary> GetSummaryAsync(string caregiverId)
    {
        var patients = (await _patientRepo.GetByCaregiverIdAsync(caregiverId)).ToList();
        var alerts = (await _alertRepo.GetByCaregiverIdAsync(caregiverId))
            .Where(a => !a.IsRead)
            .ToList();

        var summaries = new List<PatientStatusDto>();

        foreach (var patient in patients)
        {
            var patientAlerts = alerts.Where(a => a.PatientId == patient.Id).ToList();

            summaries.Add(new PatientStatusDto
            {
                PatientId = patient.Id,
                Name = patient.Name,
                LastHeartRate = patient.LastHeartRate,
                LastOxygen = patient.LastOxygen,
                LastActivity = patient.LastActivity,
                LastReadingAt = patient.LastReadingAt,
                HasDevice = !string.IsNullOrEmpty(patient.DeviceSerialNumber),
                ActiveAlerts = patientAlerts.Count
            });
        }

        return new DashboardSummary
        {
            TotalPatients = patients.Count,
            ActiveAlerts = alerts.Count,
            PatientsWithDevice = patients.Count(p => !string.IsNullOrEmpty(p.DeviceSerialNumber)),
            Patients = summaries
        };
    }

    public async Task<PatientStatusDto?> GetPatientStatusAsync(string patientId, string caregiverId)
    {
        var patient = await _patientRepo.GetByIdAsync(patientId);
        if (patient is null || patient.CaregiverId != caregiverId)
            return null;

        var activeAlerts = (await _alertRepo.GetByPatientIdAsync(patientId))
            .Count(a => !a.IsRead);

        return new PatientStatusDto
        {
            PatientId = patient.Id,
            Name = patient.Name,
            LastHeartRate = patient.LastHeartRate,
            LastOxygen = patient.LastOxygen,
            LastActivity = patient.LastActivity,
            LastReadingAt = patient.LastReadingAt,
            HasDevice = !string.IsNullOrEmpty(patient.DeviceSerialNumber),
            ActiveAlerts = activeAlerts
        };
    }
}
