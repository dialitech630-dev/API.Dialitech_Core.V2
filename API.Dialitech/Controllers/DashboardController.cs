using API.Dialitech.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Dialitech.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> GetSummary()
    {
        var caregiverId = GetCaregiverId();
        var summary = await _dashboardService.GetSummaryAsync(caregiverId);
        return Ok(summary);
    }

    [HttpGet("{patientId}")]
    public async Task<IActionResult> GetPatientStatus(string patientId)
    {
        if (string.IsNullOrWhiteSpace(patientId) || patientId.Contains('$'))
            return BadRequest("Invalid patient id");

        var caregiverId = GetCaregiverId();
        var status = await _dashboardService.GetPatientStatusAsync(patientId, caregiverId);
        if (status is null)
            return NotFound();

        return Ok(status);
    }

    [HttpGet("{patientId}/readings")]
    public async Task<IActionResult> GetPatientReadings(
        string patientId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int limit = 500)
    {
        if (string.IsNullOrWhiteSpace(patientId) || patientId.Contains('$'))
            return BadRequest("Invalid patient id");

        if (limit is < 1 or > 1000)
            return BadRequest("Limit must be between 1 and 1000");

        var caregiverId = GetCaregiverId();
        var readings = await _dashboardService.GetPatientReadingsAsync(patientId, caregiverId, from, to, limit);
        if (readings is null)
            return NotFound();

        return Ok(readings);
    }

    private string GetCaregiverId()
    {
        return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException();
    }
}
