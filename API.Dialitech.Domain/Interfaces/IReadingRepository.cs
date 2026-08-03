using API.Dialitech.Domain.Entities;

namespace API.Dialitech.Domain.Interfaces;

public interface IReadingRepository
{
    Task AddManyAsync(IEnumerable<Reading> readings);
    Task<List<Reading>> GetByPatientIdAsync(string patientId, DateTime? from, DateTime? to, int limit = 500);
}
