using API.Dialitech.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Dialitech.Controllers;

[ApiController]
[Route("api/v1/alerts")]
[Authorize]
public class AlertsController : ControllerBase
{
    private readonly IAlertService _alertService;

    public AlertsController(IAlertService alertService)
    {
        _alertService = alertService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var caregiverId = GetCaregiverId();
        var alerts = await _alertService.GetByCaregiverAsync(caregiverId);
        return Ok(alerts);
    }

    [HttpGet("{patientId}")]
    public async Task<IActionResult> GetByPatient(string patientId)
    {
        if (string.IsNullOrWhiteSpace(patientId) || patientId.Contains('$'))
            return BadRequest("Invalid patient id");

        var caregiverId = GetCaregiverId();
        var alerts = await _alertService.GetByPatientAsync(patientId, caregiverId);
        return Ok(alerts);
    }

    [HttpDelete("{alertId}")]
    public async Task<IActionResult> Delete(string alertId)
    {
        if (string.IsNullOrWhiteSpace(alertId) || alertId.Contains('$'))
            return BadRequest("Invalid alert id");

        var caregiverId = GetCaregiverId();
        await _alertService.DeleteAsync(alertId, caregiverId);
        return NoContent();
    }

    private string GetCaregiverId()
    {
        return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException();
    }
}
