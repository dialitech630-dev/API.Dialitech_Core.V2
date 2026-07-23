using API.Dialitech.Application.Commands.HealthData.CreateHealthData;
using API.Dialitech.Application.DTOs;
using API.Dialitech.Application.Interfaces;
using API.Dialitech.Application.Queries.HealthData.GetByDateRange;
using API.Dialitech.Application.Queries.HealthData.GetByUser;
using API.Dialitech.Application.Queries.HealthData.GetLatest;
using MediatR;

namespace API.Dialitech.Application.Services;

public class HealthDataService : IHealthDataService
{
    private readonly IMediator _mediator;

    public HealthDataService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<HealthDataDto> CreateAsync(CreateHealthDataDto dto)
    {
        return await _mediator.Send(new CreateHealthDataCommand(
            dto.UserId, dto.HeartRate, dto.SpO2, dto.ActivityLevel, dto.Timestamp));
    }

    public async Task<IEnumerable<HealthDataDto>> GetByUserIdAsync(string userId)
    {
        return await _mediator.Send(new GetHealthDataByUserQuery(userId));
    }

    public async Task<HealthDataDto?> GetLatestAsync(string userId)
    {
        return await _mediator.Send(new GetLatestHealthDataQuery(userId));
    }

    public async Task<IEnumerable<HealthDataDto>> GetByDateRangeAsync(string userId, DateTime? start, DateTime? end)
    {
        return await _mediator.Send(new GetHealthDataByDateRangeQuery(userId, start, end));
    }
}
