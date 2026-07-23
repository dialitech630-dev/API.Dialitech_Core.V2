using API.Dialitech.Application.DTOs;
using MediatR;

namespace API.Dialitech.Application.Queries.HealthData.GetLatest;

public record GetLatestHealthDataQuery(string UserId) : IRequest<HealthDataDto?>;
