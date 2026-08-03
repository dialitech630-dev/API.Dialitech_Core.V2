namespace API.Dialitech.Domain.Entities;

public class Reading
{
    public string Id { get; set; } = null!;
    public string PatientId { get; set; } = string.Empty;
    public string CaregiverId { get; set; } = string.Empty;
    public double HeartRate { get; set; }
    public double Oxygen { get; set; }
    public double Activity { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
