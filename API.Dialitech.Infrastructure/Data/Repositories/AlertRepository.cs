using API.Dialitech.Domain.Entities;
using API.Dialitech.Domain.Interfaces;
using MongoDB.Driver;

namespace API.Dialitech.Infrastructure.Data.Repositories;

public class AlertRepository : IAlertRepository
{
    private readonly MongoDbContext _context;

    public AlertRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Alert>> GetByUserIdAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId) || userId.Contains('$'))
            return Enumerable.Empty<Alert>();

        var filter = Builders<Alert>.Filter.Eq(a => a.UserId, userId);
        return await _context.Alerts.Find(filter).ToListAsync();
    }

    public async Task CreateAsync(Alert alert)
    {
        await _context.Alerts.InsertOneAsync(alert);
    }
}
