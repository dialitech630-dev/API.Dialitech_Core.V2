using API.Dialitech.Domain.Entities;
using API.Dialitech.Domain.Interfaces;
using MongoDB.Driver;

namespace API.Dialitech.Infrastructure.Data.Repositories;

public class CaregiverRepository : ICaregiverRepository
{
    private readonly MongoDbContext _context;

    public CaregiverRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Caregiver?> GetByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Contains('$'))
            return null;

        var filter = Builders<Caregiver>.Filter.Eq(c => c.Id, id);
        return await _context.Caregivers.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<Caregiver?> GetByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var filter = Builders<Caregiver>.Filter.Eq(c => c.Email, email.ToLowerInvariant());
        return await _context.Caregivers.Find(filter).FirstOrDefaultAsync();
    }

    public async Task CreateAsync(Caregiver caregiver)
    {
        await _context.Caregivers.InsertOneAsync(caregiver);
    }

    public async Task UpdateAsync(Caregiver caregiver)
    {
        var filter = Builders<Caregiver>.Filter.Eq(c => c.Id, caregiver.Id);
        await _context.Caregivers.ReplaceOneAsync(filter, caregiver);
    }

    public async Task DeleteAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Contains('$'))
            return;

        var filter = Builders<Caregiver>.Filter.Eq(c => c.Id, id);
        await _context.Caregivers.DeleteOneAsync(filter);
    }
}
