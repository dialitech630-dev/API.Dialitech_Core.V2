using API.Dialitech.Application.DTOs;

namespace API.Dialitech.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummary> GetSummaryAsync(string caregiverId);
    Task<PatientStatusDto?> GetPatientStatusAsync(string patientId, string caregiverId);
}
