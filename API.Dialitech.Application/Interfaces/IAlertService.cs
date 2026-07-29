using API.Dialitech.Application.DTOs;

namespace API.Dialitech.Application.Interfaces;

public interface IAlertService
{
    Task<IEnumerable<AlertDto>> GetByCaregiverAsync(string caregiverId);
    Task<IEnumerable<AlertDto>> GetByPatientAsync(string patientId, string caregiverId);
    Task DeleteAsync(string alertId, string caregiverId);
}
