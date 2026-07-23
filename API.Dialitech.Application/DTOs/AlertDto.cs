namespace API.Dialitech.Application.DTOs;

public class AlertDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int Severity { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsRead { get; set; }
}
