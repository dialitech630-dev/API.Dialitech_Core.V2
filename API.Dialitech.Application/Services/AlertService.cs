using API.Dialitech.Application.DTOs;
using API.Dialitech.Application.Interfaces;
using API.Dialitech.Application.Queries.Alerts.GetAlertsByUser;
using MediatR;

namespace API.Dialitech.Application.Services;

public class AlertService : IAlertService
{
    private readonly IMediator _mediator;

    public AlertService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IEnumerable<AlertDto>> GetByUserIdAsync(string userId)
    {
        return await _mediator.Send(new GetAlertsByUserQuery(userId));
    }
}
