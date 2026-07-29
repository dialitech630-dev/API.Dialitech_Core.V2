using API.Dialitech.Application.DTOs;

namespace API.Dialitech.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<CaregiverDto?> GetByIdAsync(string id);
}
