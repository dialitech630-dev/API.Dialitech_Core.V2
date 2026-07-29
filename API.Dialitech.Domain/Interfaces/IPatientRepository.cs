using API.Dialitech.Domain.Entities;

namespace API.Dialitech.Domain.Interfaces;

public interface IPatientRepository
{
    Task<IEnumerable<Patient>> GetByCaregiverIdAsync(string caregiverId);
    Task<Patient?> GetByIdAsync(string id);
    Task<Patient?> GetByCodeAsync(string code);
    Task<int> CountByCaregiverIdAsync(string caregiverId);
    Task CreateAsync(Patient patient);
    Task UpdateAsync(Patient patient);
    Task DeleteAsync(string id);
}
