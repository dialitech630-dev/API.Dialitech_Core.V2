using API.Dialitech.Application.DTOs;
using MediatR;

namespace API.Dialitech.Application.Queries.HealthData.GetByDateRange;

public record GetHealthDataByDateRangeQuery(
    string UserId,
    DateTime? Start,
    DateTime? End) : IRequest<IEnumerable<HealthDataDto>>;
