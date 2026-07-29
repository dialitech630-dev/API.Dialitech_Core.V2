namespace API.Dialitech.Application.DTOs;

public class GenerateCodeResponse
{
    public string Code { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
}

public class ValidateCodeRequest
{
    public string Code { get; set; } = string.Empty;
}

public class ValidateCodeResponse
{
    public bool IsValid { get; set; }
    public string PatientId { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
}

public class LinkDeviceRequest
{
    public string Code { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
}

public class LinkDeviceResponse
{
    public bool Linked { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
}
