namespace API.Dialitech.Application.DTOs;

public class HealthDataDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public int HeartRate { get; set; }
    public double SpO2 { get; set; }
    public int ActivityLevel { get; set; }
    public DateTime Timestamp { get; set; }
}

public class CreateHealthDataDto
{
    public string UserId { get; set; } = string.Empty;
    public int HeartRate { get; set; }
    public double SpO2 { get; set; }
    public int ActivityLevel { get; set; }
    public DateTime Timestamp { get; set; }
}
