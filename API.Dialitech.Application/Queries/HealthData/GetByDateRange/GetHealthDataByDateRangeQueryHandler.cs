using API.Dialitech.Application.DTOs;
using API.Dialitech.Domain.Interfaces;
using MediatR;

namespace API.Dialitech.Application.Queries.HealthData.GetByDateRange;

public class GetHealthDataByDateRangeQueryHandler : IRequestHandler<GetHealthDataByDateRangeQuery, IEnumerable<HealthDataDto>>
{
    private readonly IHealthRecordRepository _repository;

    public GetHealthDataByDateRangeQueryHandler(IHealthRecordRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<HealthDataDto>> Handle(GetHealthDataByDateRangeQuery request, CancellationToken cancellationToken)
    {
        var records = await _repository.GetByDateRangeAsync(request.UserId, request.Start, request.End);
        return records.Select(r => new HealthDataDto
        {
            Id = r.Id,
            UserId = r.UserId,
            HeartRate = r.HeartRate,
            SpO2 = r.SpO2,
            ActivityLevel = r.ActivityLevel,
            Timestamp = r.Timestamp
        });
    }
}
