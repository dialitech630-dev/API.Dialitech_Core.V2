namespace API.Dialitech.Application.DTOs;

public class BatchRequest
{
    public string PatientCode { get; set; } = string.Empty;
    public List<BatchDataPoint> Data { get; set; } = [];
}

public class BatchDataPoint
{
    public double HeartRate { get; set; }
    public double Oxygen { get; set; }
    public double Activity { get; set; }
    public DateTime Timestamp { get; set; }
}

public class BatchResponse
{
    public string Status { get; set; } = "processed";
    public int AlertsTriggered { get; set; }
}

public class PatientInfoResponse
{
    public string PatientCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? DeviceSerialNumber { get; set; }
    public double? LastHeartRate { get; set; }
    public double? LastOxygen { get; set; }
    public double? LastActivity { get; set; }
    public DateTime? LastReadingAt { get; set; }
}
