using API.Dialitech.Domain.Entities;

namespace API.Dialitech.Domain.Interfaces;

public interface IHealthRecordRepository
{
    Task<IEnumerable<HealthRecord>> GetByUserIdAsync(string userId);
    Task<HealthRecord?> GetByIdAsync(string id);
    Task<IEnumerable<HealthRecord>> GetByDateRangeAsync(string userId, DateTime? start, DateTime? end);
    Task<HealthRecord?> GetLatestAsync(string userId);
    Task CreateAsync(HealthRecord record);
    Task DeleteAsync(string id);
}
