using API.Dialitech.Application.DTOs;
using MediatR;

namespace API.Dialitech.Application.Queries.HealthData.GetByUser;

public record GetHealthDataByUserQuery(string UserId) : IRequest<IEnumerable<HealthDataDto>>;
