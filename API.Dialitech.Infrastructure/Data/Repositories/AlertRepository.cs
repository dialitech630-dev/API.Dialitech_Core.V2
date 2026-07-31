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

    public async Task<IEnumerable<Alert>> GetByCaregiverIdAsync(string caregiverId)
    {
        if (string.IsNullOrWhiteSpace(caregiverId) || caregiverId.Contains('$'))
            return Enumerable.Empty<Alert>();

        var filter = Builders<Alert>.Filter.Eq(a => a.CaregiverId, caregiverId);
        return await _context.Alerts.Find(filter)
            .SortByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Alert>> GetByPatientIdAsync(string patientId)
    {
        if (string.IsNullOrWhiteSpace(patientId) || patientId.Contains('$'))
            return Enumerable.Empty<Alert>();

        var filter = Builders<Alert>.Filter.Eq(a => a.PatientId, patientId);
        return await _context.Alerts.Find(filter)
            .SortByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task CreateAsync(Alert alert)
    {
        await _context.Alerts.InsertOneAsync(alert);
    }

    public async Task DeleteAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Contains('$'))
            return;

        var filter = Builders<Alert>.Filter.Eq(a => a.Id, id);
        await _context.Alerts.DeleteOneAsync(filter);
    }

    public async Task DeleteByPatientIdAsync(string patientId)
    {
        if (string.IsNullOrWhiteSpace(patientId))
            return;

        var filter = Builders<Alert>.Filter.Eq(a => a.PatientId, patientId);
        await _context.Alerts.DeleteManyAsync(filter);
    }
}
