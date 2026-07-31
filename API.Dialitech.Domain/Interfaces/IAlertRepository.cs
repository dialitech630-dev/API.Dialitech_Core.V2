using API.Dialitech.Domain.Entities;

namespace API.Dialitech.Domain.Interfaces;

public interface IAlertRepository
{
    Task<IEnumerable<Alert>> GetByCaregiverIdAsync(string caregiverId);
    Task<IEnumerable<Alert>> GetByPatientIdAsync(string patientId);
    Task CreateAsync(Alert alert);
    Task DeleteAsync(string id);
    Task DeleteByPatientIdAsync(string patientId);
}
