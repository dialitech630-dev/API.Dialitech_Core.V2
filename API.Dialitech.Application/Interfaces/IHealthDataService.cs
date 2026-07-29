using API.Dialitech.Application.DTOs;

namespace API.Dialitech.Application.Interfaces;

public interface IHealthDataService
{
    Task<BatchResponse> ProcessBatchAsync(BatchRequest request);
}
