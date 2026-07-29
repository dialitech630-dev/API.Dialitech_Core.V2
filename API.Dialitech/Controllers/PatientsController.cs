using API.Dialitech.Application.DTOs;
using API.Dialitech.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Dialitech.Controllers;

[ApiController]
[Route("api/v1/patients")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly IPatientService _patientService;

    public PatientsController(IPatientService patientService)
    {
        _patientService = patientService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var caregiverId = GetCaregiverId();
        var patients = await _patientService.GetAllAsync(caregiverId);
        return Ok(patients);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Contains('$'))
            return BadRequest("Invalid id");

        var caregiverId = GetCaregiverId();
        var patient = await _patientService.GetByIdAsync(id, caregiverId);
        if (patient is null)
            return NotFound();

        return Ok(patient);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePatientRequest request)
    {
        var caregiverId = GetCaregiverId();
        var patient = await _patientService.CreateAsync(caregiverId, request);
        return CreatedAtAction(nameof(GetById), new { id = patient.Id }, patient);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Contains('$'))
            return BadRequest("Invalid id");

        var caregiverId = GetCaregiverId();
        await _patientService.DeleteAsync(id, caregiverId);
        return NoContent();
    }

    private string GetCaregiverId()
    {
        return User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException();
    }
}
