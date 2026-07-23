namespace API.Dialitech.Domain.Entities;

public class User
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int Age { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
