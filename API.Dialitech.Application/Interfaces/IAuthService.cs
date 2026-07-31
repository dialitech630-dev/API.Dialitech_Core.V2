using API.Dialitech.Application.DTOs;

namespace API.Dialitech.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<CaregiverDto?> GetByIdAsync(string id);
    Task<CaregiverDto> UpdateProfileAsync(string caregiverId, UpdateProfileRequest request);
    Task DeleteAccountAsync(string caregiverId);
    Task ChangePasswordAsync(string caregiverId, ChangePasswordRequest request);
    Task<string> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task ResetPasswordAsync(ResetPasswordRequest request);
    Task<CaregiverDto> ChangePlanAsync(string caregiverId, ChangePlanRequest request);
}
