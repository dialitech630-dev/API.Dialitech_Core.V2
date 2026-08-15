using API.Dialitech.Application.DTOs;
using API.Dialitech.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Dialitech.Controllers;

[ApiController]
[Route("api/v1")]
public class DevicesController : ControllerBase
{
    private readonly IDeviceService _deviceService;

    public DevicesController(IDeviceService deviceService)
    {
        _deviceService = deviceService;
    }

    [HttpPost("patients/{id}/generate-code")]
    [Authorize]
    public async Task<IActionResult> GenerateCode(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Contains('$'))
            return BadRequest("Invalid patient id");

        var caregiverId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException();

        var result = await _deviceService.GenerateCodeAsync(id, caregiverId);
        return Ok(result);
    }

    [HttpPost("patients/{id}/generate-wearable-code")]
    [Authorize]
    public async Task<IActionResult> GenerateWearableCode(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Contains('$'))
            return BadRequest("Invalid patient id");

        var caregiverId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException();

        var result = await _deviceService.GenerateWearableCodeAsync(id, caregiverId);
        return Ok(result);
    }

    [HttpPost("patients/validate-code")]
    [EnableRateLimiting("sensitive")]
    public async Task<IActionResult> ValidateCode([FromBody] ValidateCodeRequest request)
    {
        var result = await _deviceService.ValidateCodeAsync(request.Code);
        return Ok(result);
    }

    [HttpPost("devices/link")]
    [EnableRateLimiting("sensitive")]
    public async Task<IActionResult> LinkDevice([FromBody] LinkDeviceRequest request)
    {
        var result = await _deviceService.LinkDeviceAsync(request.Code, request.SerialNumber);
        return Ok(result);
    }
}
