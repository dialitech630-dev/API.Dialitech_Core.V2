using API.Dialitech.Application.DTOs;

namespace API.Dialitech.Application.Interfaces;

public interface IDeviceService
{
    Task<GenerateCodeResponse> GenerateCodeAsync(string patientId, string caregiverId);
    Task<GenerateCodeResponse> GenerateWearableCodeAsync(string patientId, string caregiverId);
    Task<ValidateCodeResponse> ValidateCodeAsync(string code);
    Task<LinkDeviceResponse> LinkDeviceAsync(string code, string serialNumber);
}
