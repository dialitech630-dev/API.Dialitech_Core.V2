using API.Dialitech.Domain.Entities;

namespace API.Dialitech.Domain.Interfaces;

public interface ICaregiverRepository
{
    Task<Caregiver?> GetByIdAsync(string id);
    Task<Caregiver?> GetByEmailAsync(string email);
    Task CreateAsync(Caregiver caregiver);
    Task UpdateAsync(Caregiver caregiver);
    Task DeleteAsync(string id);
}
