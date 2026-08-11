namespace API.Dialitech.Domain.Entities;

public class Patient
{
    public string Id { get; set; } = null!;
    public string CaregiverId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string? Code { get; set; }
    public DateTime? CodeExpiresAt { get; set; }
    public string? WearableCode { get; set; }
    public DateTime? WearableCodeExpiresAt { get; set; }
    public string? DeviceToken { get; set; }
    public DateTime? DeviceTokenUpdatedAt { get; set; }
    public string? DeviceSerialNumber { get; set; }
    public double? LastHeartRate { get; set; }
    public double? LastOxygen { get; set; }
    public double? LastActivity { get; set; }
    public DateTime? LastReadingAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
