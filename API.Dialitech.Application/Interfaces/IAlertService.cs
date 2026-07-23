using API.Dialitech.Application.DTOs;

namespace API.Dialitech.Application.Interfaces;

public interface IAlertService
{
    Task<IEnumerable<AlertDto>> GetByUserIdAsync(string userId);
}
