using API.Dialitech.Application.DTOs;
using API.Dialitech.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace API.Dialitech.Controllers;

[ApiController]
[EnableRateLimiting("batch")]
[Route("api/v1/health-data")]
public class HealthDataController : ControllerBase
{
    private readonly IHealthDataService _healthDataService;

    public HealthDataController(IHealthDataService healthDataService)
    {
        _healthDataService = healthDataService;
    }

    [HttpPost("batch")]
    public async Task<IActionResult> ProcessBatch([FromBody] BatchRequest request)
    {
        var result = await _healthDataService.ProcessBatchAsync(request);
        return Ok(result);
    }
}
