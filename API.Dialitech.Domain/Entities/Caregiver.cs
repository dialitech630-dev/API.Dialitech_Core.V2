using API.Dialitech.Domain.Enums;

namespace API.Dialitech.Domain.Entities;

public class Caregiver
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? ResetCode { get; set; }
    public DateTime? ResetCodeExpiresAt { get; set; }
    public Plan Plan { get; set; } = Plan.Standard;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
