using API.Dialitech.Application.Commands.HealthData.CreateHealthData;
using API.Dialitech.Application.DTOs;
using API.Dialitech.Application.Queries.HealthData.GetByDateRange;
using API.Dialitech.Application.Queries.HealthData.GetByUser;
using API.Dialitech.Application.Queries.HealthData.GetLatest;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Dialitech.Controllers;

[ApiController]
[Route("api/health-data")]
[Authorize]
public class HealthDataController : ControllerBase
{
    private readonly ISender _sender;

    public HealthDataController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateHealthDataDto dto)
    {
        var command = new CreateHealthDataCommand(
            dto.UserId, dto.HeartRate, dto.SpO2, dto.ActivityLevel, dto.Timestamp);
        var result = await _sender.Send(command);
        return CreatedAtAction(null, result);
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetByUser(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId) || userId.Contains('$'))
            return BadRequest("Invalid userId");

        var query = new GetHealthDataByUserQuery(userId);
        var result = await _sender.Send(query);
        return Ok(result);
    }

    [HttpGet("{userId}/latest")]
    public async Task<IActionResult> GetLatest(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId) || userId.Contains('$'))
            return BadRequest("Invalid userId");

        var query = new GetLatestHealthDataQuery(userId);
        var result = await _sender.Send(query);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpGet("{userId}/range")]
    public async Task<IActionResult> GetByDateRange(
        string userId,
        [FromQuery] DateTime? start,
        [FromQuery] DateTime? end)
    {
        if (string.IsNullOrWhiteSpace(userId) || userId.Contains('$'))
            return BadRequest("Invalid userId");

        var query = new GetHealthDataByDateRangeQuery(userId, start, end);
        var result = await _sender.Send(query);
        return Ok(result);
    }
}
