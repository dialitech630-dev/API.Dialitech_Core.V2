using API.Dialitech.Domain.Entities;

namespace API.Dialitech.Domain.Interfaces;

public interface IAlertRepository
{
    Task<IEnumerable<Alert>> GetByUserIdAsync(string userId);
    Task CreateAsync(Alert alert);
}
