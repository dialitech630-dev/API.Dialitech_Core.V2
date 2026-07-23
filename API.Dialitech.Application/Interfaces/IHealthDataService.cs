using API.Dialitech.Application.DTOs;

namespace API.Dialitech.Application.Interfaces;

public interface IHealthDataService
{
    Task<HealthDataDto> CreateAsync(CreateHealthDataDto dto);
    Task<IEnumerable<HealthDataDto>> GetByUserIdAsync(string userId);
    Task<HealthDataDto?> GetLatestAsync(string userId);
    Task<IEnumerable<HealthDataDto>> GetByDateRangeAsync(string userId, DateTime? start, DateTime? end);
}
