using API.Dialitech.Application.DTOs;

namespace API.Dialitech.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummary> GetSummaryAsync(string caregiverId);
    Task<PatientStatusDto?> GetPatientStatusAsync(string patientId, string caregiverId);
    Task<ReadingsResponse?> GetPatientReadingsAsync(
        string patientId, string caregiverId, DateTime? from, DateTime? to, int limit = 500);
}
