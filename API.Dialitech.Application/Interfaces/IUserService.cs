using API.Dialitech.Application.DTOs;

namespace API.Dialitech.Application.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserDto>> GetAllAsync();
    Task<UserDto?> GetByIdAsync(string id);
    Task CreateAsync(CreateUserDto dto);
    Task UpdateAsync(string id, UpdateUserDto dto);
    Task DeleteAsync(string id);
    Task<UserDto> RegisterAsync(RegisterRequest request);
    Task<UserDto?> LoginAsync(LoginRequest request);
}
