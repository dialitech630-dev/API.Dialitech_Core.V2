namespace API.Dialitech.Domain.Entities;

public class Device
{
    public string Id { get; set; } = null!;
    public string PatientId { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? LastSeenAt { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}
