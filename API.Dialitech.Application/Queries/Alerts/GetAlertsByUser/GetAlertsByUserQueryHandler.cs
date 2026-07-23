using API.Dialitech.Application.DTOs;
using API.Dialitech.Domain.Interfaces;
using MediatR;

namespace API.Dialitech.Application.Queries.Alerts.GetAlertsByUser;

public class GetAlertsByUserQueryHandler : IRequestHandler<GetAlertsByUserQuery, IEnumerable<AlertDto>>
{
    private readonly IAlertRepository _repository;

    public GetAlertsByUserQueryHandler(IAlertRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<AlertDto>> Handle(GetAlertsByUserQuery request, CancellationToken cancellationToken)
    {
        var alerts = await _repository.GetByUserIdAsync(request.UserId);
        return alerts.Select(a => new AlertDto
        {
            Id = a.Id,
            UserId = a.UserId,
            Type = a.Type,
            Message = a.Message,
            Severity = a.Severity,
            Timestamp = a.Timestamp,
            IsRead = a.IsRead
        });
    }
}
