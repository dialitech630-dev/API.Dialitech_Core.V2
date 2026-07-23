using API.Dialitech.Application.DTOs;
using MediatR;

namespace API.Dialitech.Application.Queries.Alerts.GetAlertsByUser;

public record GetAlertsByUserQuery(string UserId) : IRequest<IEnumerable<AlertDto>>;
