namespace API.Dialitech.Application.DTOs;

public class DashboardSummary
{
    public int TotalPatients { get; set; }
    public int ActiveAlerts { get; set; }
    public int PatientsWithDevice { get; set; }
    public List<PatientStatusDto> Patients { get; set; } = [];
}

public class PatientStatusDto
{
    public string PatientId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double? LastHeartRate { get; set; }
    public double? LastOxygen { get; set; }
    public double? LastActivity { get; set; }
    public DateTime? LastReadingAt { get; set; }
    public bool HasDevice { get; set; }
    public int ActiveAlerts { get; set; }
}
