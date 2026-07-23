namespace API.Dialitech.Domain.Entities;

public class Alert
{
    public string Id { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int Severity { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsRead { get; set; }
}
