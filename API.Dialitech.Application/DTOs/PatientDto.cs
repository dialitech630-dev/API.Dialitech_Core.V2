namespace API.Dialitech.Application.DTOs;

public class PatientDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string? DeviceSerialNumber { get; set; }
    public double? LastHeartRate { get; set; }
    public double? LastOxygen { get; set; }
    public double? LastActivity { get; set; }
    public DateTime? LastReadingAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreatePatientRequest
{
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
