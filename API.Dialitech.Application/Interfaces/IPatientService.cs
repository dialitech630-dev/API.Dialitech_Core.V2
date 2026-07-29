using API.Dialitech.Application.DTOs;

namespace API.Dialitech.Application.Interfaces;

public interface IPatientService
{
    Task<IEnumerable<PatientDto>> GetAllAsync(string caregiverId);
    Task<PatientDto?> GetByIdAsync(string id, string caregiverId);
    Task<PatientDto> CreateAsync(string caregiverId, CreatePatientRequest request);
    Task DeleteAsync(string id, string caregiverId);
}
