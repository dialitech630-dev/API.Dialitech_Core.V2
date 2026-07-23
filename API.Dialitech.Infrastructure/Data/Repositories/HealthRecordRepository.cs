using API.Dialitech.Domain.Entities;
using API.Dialitech.Domain.Interfaces;
using MongoDB.Driver;

namespace API.Dialitech.Infrastructure.Data.Repositories;

public class HealthRecordRepository : IHealthRecordRepository
{
    private readonly MongoDbContext _context;

    public HealthRecordRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<HealthRecord>> GetByUserIdAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId) || userId.Contains('$'))
            return Enumerable.Empty<HealthRecord>();

        var filter = Builders<HealthRecord>.Filter.Eq(r => r.UserId, userId);
        return await _context.HealthRecords.Find(filter).ToListAsync();
    }

    public async Task<HealthRecord?> GetByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Contains('$'))
            return null;

        var filter = Builders<HealthRecord>.Filter.Eq(r => r.Id, id);
        return await _context.HealthRecords.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<HealthRecord>> GetByDateRangeAsync(string userId, DateTime? start, DateTime? end)
    {
        if (string.IsNullOrWhiteSpace(userId) || userId.Contains('$'))
            return Enumerable.Empty<HealthRecord>();

        var builder = Builders<HealthRecord>.Filter;
        var filter = builder.Eq(r => r.UserId, userId);

        if (start.HasValue)
            filter &= builder.Gte(r => r.Timestamp, start.Value);

        if (end.HasValue)
            filter &= builder.Lte(r => r.Timestamp, end.Value);

        return await _context.HealthRecords.Find(filter).ToListAsync();
    }

    public async Task<HealthRecord?> GetLatestAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId) || userId.Contains('$'))
            return null;

        var filter = Builders<HealthRecord>.Filter.Eq(r => r.UserId, userId);
        return await _context.HealthRecords
            .Find(filter)
            .SortByDescending(r => r.Timestamp)
            .FirstOrDefaultAsync();
    }

    public async Task CreateAsync(HealthRecord record)
    {
        await _context.HealthRecords.InsertOneAsync(record);
    }

    public async Task DeleteAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Contains('$'))
            return;

        var filter = Builders<HealthRecord>.Filter.Eq(r => r.Id, id);
        await _context.HealthRecords.DeleteOneAsync(filter);
    }
}
