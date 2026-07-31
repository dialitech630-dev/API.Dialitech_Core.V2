namespace API.Dialitech.Application.DTOs;

public class RegisterRequest
{
    public string Name { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Plan { get; set; } = "Standard";
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public CaregiverDto Caregiver { get; set; } = null!;
}

public class UpdateProfileRequest
{
    public string Name { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}

public class CaregiverDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
}
