using API.Dialitech.Application.DTOs;
using API.Dialitech.Domain.Interfaces;
using MediatR;

namespace API.Dialitech.Application.Queries.HealthData.GetLatest;

public class GetLatestHealthDataQueryHandler : IRequestHandler<GetLatestHealthDataQuery, HealthDataDto?>
{
    private readonly IHealthRecordRepository _repository;

    public GetLatestHealthDataQueryHandler(IHealthRecordRepository repository)
    {
        _repository = repository;
    }

    public async Task<HealthDataDto?> Handle(GetLatestHealthDataQuery request, CancellationToken cancellationToken)
    {
        var record = await _repository.GetLatestAsync(request.UserId);
        if (record is null) return null;

        return new HealthDataDto
        {
            Id = record.Id,
            UserId = record.UserId,
            HeartRate = record.HeartRate,
            SpO2 = record.SpO2,
            ActivityLevel = record.ActivityLevel,
            Timestamp = record.Timestamp
        };
    }
}
