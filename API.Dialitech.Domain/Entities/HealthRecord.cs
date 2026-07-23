namespace API.Dialitech.Domain.Entities;

public class HealthRecord
{
    public string Id { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public int HeartRate { get; set; }
    public double SpO2 { get; set; }
    public int ActivityLevel { get; set; }
    public DateTime Timestamp { get; set; }
}
