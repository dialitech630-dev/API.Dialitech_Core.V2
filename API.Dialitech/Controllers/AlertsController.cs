using API.Dialitech.Application.Queries.Alerts.GetAlertsByUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Dialitech.Controllers;

[ApiController]
[Route("api/alerts")]
[Authorize]
public class AlertsController : ControllerBase
{
    private readonly ISender _sender;

    public AlertsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetByUser(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId) || userId.Contains('$'))
            return BadRequest("Invalid userId");

        var query = new GetAlertsByUserQuery(userId);
        var result = await _sender.Send(query);
        return Ok(result);
    }
}
