using System.Collections.Concurrent;
using API.Dialitech.Domain.Entities;
using API.Dialitech.Domain.Interfaces;

namespace API.Dialitech.IntegrationTest;

public class InMemoryCaregiverRepository : ICaregiverRepository
{
    private readonly ConcurrentDictionary<string, Caregiver> _store = new();

    public Task<Caregiver?> GetByIdAsync(string id)
    {
        _store.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<Caregiver?> GetByEmailAsync(string email)
    {
        var entity = _store.Values.FirstOrDefault(c =>
            string.Equals(c.Email, email, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(entity);
    }

    public Task CreateAsync(Caregiver caregiver)
    {
        caregiver.Id ??= Guid.NewGuid().ToString("N");
        _store[caregiver.Id] = caregiver;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Caregiver caregiver)
    {
        _store[caregiver.Id] = caregiver;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id)
    {
        _store.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}

public class InMemoryPatientRepository : IPatientRepository
{
    private readonly ConcurrentDictionary<string, Patient> _store = new();

    public Task<IEnumerable<Patient>> GetByCaregiverIdAsync(string caregiverId)
    {
        var items = _store.Values.Where(p => p.CaregiverId == caregiverId);
        return Task.FromResult(items.AsEnumerable());
    }

    public Task<Patient?> GetByIdAsync(string id)
    {
        _store.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<Patient?> GetByCodeAsync(string code)
    {
        var entity = _store.Values.FirstOrDefault(p =>
            string.Equals(p.Code, code, StringComparison.Ordinal) ||
            string.Equals(p.WearableCode, code, StringComparison.Ordinal));
        return Task.FromResult(entity);
    }

    public Task<int> CountByCaregiverIdAsync(string caregiverId)
    {
        var count = _store.Values.Count(p => p.CaregiverId == caregiverId);
        return Task.FromResult(count);
    }

    public Task CreateAsync(Patient patient)
    {
        patient.Id ??= Guid.NewGuid().ToString("N");
        _store[patient.Id] = patient;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Patient patient)
    {
        _store[patient.Id] = patient;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id)
    {
        _store.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}

public class InMemoryDeviceRepository : IDeviceRepository
{
    private readonly ConcurrentDictionary<string, Device> _store = new();

    public Task<Device?> GetBySerialNumberAsync(string serialNumber)
    {
        var entity = _store.Values.FirstOrDefault(d =>
            string.Equals(d.SerialNumber, serialNumber, StringComparison.Ordinal));
        return Task.FromResult(entity);
    }

    public Task<Device?> GetByPatientIdAsync(string patientId)
    {
        var entity = _store.Values.FirstOrDefault(d =>
            string.Equals(d.PatientId, patientId, StringComparison.Ordinal));
        return Task.FromResult(entity);
    }

    public Task CreateAsync(Device device)
    {
        device.Id ??= Guid.NewGuid().ToString("N");
        _store[device.Id] = device;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Device device)
    {
        _store[device.Id] = device;
        return Task.CompletedTask;
    }

    public Task DeleteByPatientIdAsync(string patientId)
    {
        var toRemove = _store.Values.Where(d =>
            string.Equals(d.PatientId, patientId, StringComparison.Ordinal)).ToList();
        foreach (var d in toRemove)
            _store.TryRemove(d.Id, out _);
        return Task.CompletedTask;
    }
}

public class InMemoryAlertRepository : IAlertRepository
{
    private readonly ConcurrentDictionary<string, Alert> _store = new();

    public Task<IEnumerable<Alert>> GetByCaregiverIdAsync(string caregiverId)
    {
        var items = _store.Values.Where(a =>
            string.Equals(a.CaregiverId, caregiverId, StringComparison.Ordinal));
        return Task.FromResult(items.AsEnumerable());
    }

    public Task<IEnumerable<Alert>> GetByPatientIdAsync(string patientId)
    {
        var items = _store.Values.Where(a =>
            string.Equals(a.PatientId, patientId, StringComparison.Ordinal));
        return Task.FromResult(items.AsEnumerable());
    }

    public Task CreateAsync(Alert alert)
    {
        alert.Id ??= Guid.NewGuid().ToString("N");
        _store[alert.Id] = alert;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id)
    {
        _store.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task DeleteByPatientIdAsync(string patientId)
    {
        var toRemove = _store.Values.Where(a =>
            string.Equals(a.PatientId, patientId, StringComparison.Ordinal)).ToList();
        foreach (var a in toRemove)
            _store.TryRemove(a.Id, out _);
        return Task.CompletedTask;
    }
}

public class InMemoryReadingRepository : IReadingRepository
{
    private readonly ConcurrentDictionary<string, Reading> _store = new();

    public Task AddManyAsync(IEnumerable<Reading> readings)
    {
        foreach (var reading in readings)
        {
            reading.Id ??= Guid.NewGuid().ToString("N");
            _store[reading.Id] = reading;
        }
        return Task.CompletedTask;
    }

    public Task<List<Reading>> GetByPatientIdAsync(string patientId, DateTime? from, DateTime? to, int limit = 500)
    {
        var query = _store.Values.Where(r =>
            string.Equals(r.PatientId, patientId, StringComparison.Ordinal));

        if (from.HasValue)
            query = query.Where(r => r.Timestamp >= from.Value);
        if (to.HasValue)
            query = query.Where(r => r.Timestamp <= to.Value);

        var result = query.OrderByDescending(r => r.Timestamp).Take(limit).ToList();
        return Task.FromResult(result);
    }
}
