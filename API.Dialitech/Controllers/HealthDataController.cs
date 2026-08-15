using API.Dialitech.Application.DTOs;
using API.Dialitech.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Dialitech.Controllers;

[ApiController]
[Route("api/v1/health-data")]
public class HealthDataController : ControllerBase
{
    private readonly IHealthDataService _healthDataService;

    public HealthDataController(IHealthDataService healthDataService)
    {
        _healthDataService = healthDataService;
    }

    [HttpPost("batch")]
    [EnableRateLimiting("batch")]
    public async Task<IActionResult> ProcessBatch([FromBody] BatchRequest request)
    {
        var result = await _healthDataService.ProcessBatchAsync(request);
        return Ok(result);
    }

    [HttpGet("patient-info/{patientCode}")]
    [EnableRateLimiting("sensitive")]
    public async Task<IActionResult> GetPatientInfo(string patientCode)
    {
        var result = await _healthDataService.GetPatientInfoAsync(patientCode);
        return Ok(result);
    }

    [HttpPost("device-token")]
    [EnableRateLimiting("sensitive")]
    public async Task<IActionResult> RegisterDeviceToken([FromBody] DeviceTokenRequest request)
    {
        await _healthDataService.RegisterDeviceTokenAsync(request.PatientCode, request.DeviceToken);
        return NoContent();
    }
}
