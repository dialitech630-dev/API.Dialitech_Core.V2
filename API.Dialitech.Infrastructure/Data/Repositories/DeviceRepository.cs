using API.Dialitech.Domain.Entities;
using API.Dialitech.Domain.Interfaces;
using MongoDB.Driver;

namespace API.Dialitech.Infrastructure.Data.Repositories;

public class DeviceRepository : IDeviceRepository
{
    private readonly MongoDbContext _context;

    public DeviceRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Device?> GetBySerialNumberAsync(string serialNumber)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
            return null;

        var filter = Builders<Device>.Filter.Eq(d => d.SerialNumber, serialNumber);
        return await _context.Devices.Find(filter).FirstOrDefaultAsync();
    }

    public async Task<Device?> GetByPatientIdAsync(string patientId)
    {
        if (string.IsNullOrWhiteSpace(patientId))
            return null;

        var filter = Builders<Device>.Filter.Eq(d => d.PatientId, patientId);
        return await _context.Devices.Find(filter).FirstOrDefaultAsync();
    }

    public async Task CreateAsync(Device device)
    {
        await _context.Devices.InsertOneAsync(device);
    }

    public async Task UpdateAsync(Device device)
    {
        var filter = Builders<Device>.Filter.Eq(d => d.Id, device.Id);
        await _context.Devices.ReplaceOneAsync(filter, device);
    }

    public async Task DeleteByPatientIdAsync(string patientId)
    {
        if (string.IsNullOrWhiteSpace(patientId))
            return;

        var filter = Builders<Device>.Filter.Eq(d => d.PatientId, patientId);
        await _context.Devices.DeleteManyAsync(filter);
    }
}
