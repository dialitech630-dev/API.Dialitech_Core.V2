using API.Dialitech.Domain.Entities;
using API.Dialitech.Domain.Interfaces;
using MongoDB.Driver;

namespace API.Dialitech.Infrastructure.Data.Repositories;

public class PatientRepository : IPatientRepository
{
    private readonly MongoDbContext _context;

    public PatientRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Patient>> GetByCaregiverIdAsync(string caregiverId)
    {
        if (string.IsNullOrWhiteSpace(caregiverId) || caregiverId.Contains('$'))
            return Enumerable.Empty<Patient>();

        var filter = Builders<Patient>.Filter.Eq(p => p.CaregiverId, caregiverId);
        return await _context.Patients.Find(filter).ToListAsync();
    }

    public async Task<Patient?> GetByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Contains('$'))
            return null;

        var filter = Builders<Patient>.Filter.Eq(p => p.Id, id);
        return await _context.Patients.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<Patient?> GetByCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var filter = Builders<Patient>.Filter.Or(
            Builders<Patient>.Filter.Eq(p => p.Code, code),
            Builders<Patient>.Filter.Eq(p => p.WearableCode, code));
        return await _context.Patients.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<int> CountByCaregiverIdAsync(string caregiverId)
    {
        if (string.IsNullOrWhiteSpace(caregiverId))
            return 0;

        var filter = Builders<Patient>.Filter.Eq(p => p.CaregiverId, caregiverId);
        return (int)await _context.Patients.CountDocumentsAsync(filter);
    }

    public async Task CreateAsync(Patient patient)
    {
        await _context.Patients.InsertOneAsync(patient);
    }

    public async Task UpdateAsync(Patient patient)
    {
        var filter = Builders<Patient>.Filter.Eq(p => p.Id, patient.Id);
        await _context.Patients.ReplaceOneAsync(filter, patient);
    }

    public async Task DeleteAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Contains('$'))
            return;

        var filter = Builders<Patient>.Filter.Eq(p => p.Id, id);
        await _context.Patients.DeleteOneAsync(filter);
    }
}
