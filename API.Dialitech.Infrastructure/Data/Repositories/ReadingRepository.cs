using API.Dialitech.Domain.Entities;
using API.Dialitech.Domain.Interfaces;
using MongoDB.Driver;

namespace API.Dialitech.Infrastructure.Data.Repositories;

public class ReadingRepository : IReadingRepository
{
    private readonly MongoDbContext _context;

    public ReadingRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task AddManyAsync(IEnumerable<Reading> readings)
    {
        var list = readings.ToList();
        if (list.Count == 0)
            return;

        await _context.Readings.InsertManyAsync(list);
    }

    public async Task<List<Reading>> GetByPatientIdAsync(
        string patientId, DateTime? from, DateTime? to, int limit = 500)
    {
        if (string.IsNullOrWhiteSpace(patientId) || patientId.Contains('$'))
            return [];

        var filterBuilder = Builders<Reading>.Filter;
        var filter = filterBuilder.Eq(r => r.PatientId, patientId);

        if (from is not null)
            filter &= filterBuilder.Gte(r => r.Timestamp, from.Value);

        if (to is not null)
            filter &= filterBuilder.Lte(r => r.Timestamp, to.Value);

        return await _context.Readings.Find(filter)
            .SortByDescending(r => r.Timestamp)
            .Limit(Math.Max(1, limit))
            .ToListAsync();
    }
}
