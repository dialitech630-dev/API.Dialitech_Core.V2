using API.Dialitech.Domain.Entities;

namespace API.Dialitech.Domain.Interfaces;

public interface IDeviceRepository
{
    Task<Device?> GetBySerialNumberAsync(string serialNumber);
    Task<Device?> GetByPatientIdAsync(string patientId);
    Task CreateAsync(Device device);
    Task UpdateAsync(Device device);
}
