using API.Dialitech.Application.DTOs;
using API.Dialitech.Domain.Interfaces;
using MediatR;

namespace API.Dialitech.Application.Queries.HealthData.GetByUser;

public class GetHealthDataByUserQueryHandler : IRequestHandler<GetHealthDataByUserQuery, IEnumerable<HealthDataDto>>
{
    private readonly IHealthRecordRepository _repository;

    public GetHealthDataByUserQueryHandler(IHealthRecordRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<HealthDataDto>> Handle(GetHealthDataByUserQuery request, CancellationToken cancellationToken)
    {
        var records = await _repository.GetByUserIdAsync(request.UserId);
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
