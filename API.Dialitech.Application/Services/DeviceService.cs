using API.Dialitech.Application.Common.Exceptions;
using API.Dialitech.Application.DTOs;
using API.Dialitech.Application.Interfaces;
using API.Dialitech.Domain.Entities;
using API.Dialitech.Domain.Interfaces;

namespace API.Dialitech.Application.Services;

public class DeviceService : IDeviceService
{
    private readonly IPatientRepository _patientRepo;
    private readonly IDeviceRepository _deviceRepo;

    private const int CodeExpirySeconds = 300;

    public DeviceService(IPatientRepository patientRepo, IDeviceRepository deviceRepo)
    {
        _patientRepo = patientRepo;
        _deviceRepo = deviceRepo;
    }

    public async Task<GenerateCodeResponse> GenerateCodeAsync(string patientId, string caregiverId)
    {
        var patient = await _patientRepo.GetByIdAsync(patientId)
            ??             throw new NotFoundException("Patient", patientId);

        if (patient.CaregiverId != caregiverId)
            throw new NotFoundException("Patient", patientId);

        var code = Random.Shared.Next(100000, 999999).ToString("D6");
        patient.Code = code;
        patient.CodeExpiresAt = DateTime.UtcNow.AddSeconds(CodeExpirySeconds);
        patient.CodeUsedAt = null;
        await _patientRepo.UpdateAsync(patient);

        return new GenerateCodeResponse
        {
            Code = code,
            ExpiresInSeconds = CodeExpirySeconds
        };
    }

    public async Task<GenerateCodeResponse> GenerateWearableCodeAsync(string patientId, string caregiverId)
    {
        var patient = await _patientRepo.GetByIdAsync(patientId)
            ?? throw new NotFoundException("Patient", patientId);

        if (patient.CaregiverId != caregiverId)
            throw new ValidationException("Patient", "Patient does not belong to this caregiver.");

        var code = Random.Shared.Next(100000, 999999).ToString("D6");
        patient.WearableCode = code;
        patient.WearableCodeExpiresAt = DateTime.UtcNow.AddSeconds(CodeExpirySeconds);
        patient.WearableCodeUsedAt = null;
        await _patientRepo.UpdateAsync(patient);

        return new GenerateCodeResponse
        {
            Code = code,
            ExpiresInSeconds = CodeExpirySeconds
        };
    }

    public async Task<ValidateCodeResponse> ValidateCodeAsync(string code)
    {
        var patient = await _patientRepo.GetByCodeAsync(code);
        if (patient is null)
            return new ValidateCodeResponse { IsValid = false };

        var (expiresAt, usedAt) = GetCodeState(patient, code);

        if (expiresAt is null || expiresAt < DateTime.UtcNow || usedAt is not null)
            return new ValidateCodeResponse { IsValid = false };

        return new ValidateCodeResponse
        {
            IsValid = true,
            PatientId = patient.Id,
            PatientName = patient.Name
        };
    }

    public async Task<LinkDeviceResponse> LinkDeviceAsync(string code, string serialNumber)
    {
        var patient = await _patientRepo.GetByCodeAsync(code)
            ?? throw new ValidationException("Code", "Invalid code.");

        var (expiresAt, usedAt) = GetCodeState(patient, code);

        if (expiresAt is null || expiresAt < DateTime.UtcNow)
            throw new ValidationException("Code", "Code has expired.");

        if (usedAt is not null)
        {
            if (patient.DeviceSerialNumber == serialNumber)
            {
                return new LinkDeviceResponse
                {
                    Linked = true,
                    SerialNumber = serialNumber,
                    PatientId = patient.Id
                };
            }

            throw new ValidationException("Code", "Code has already been used.");
        }

        var existingDevice = await _deviceRepo.GetBySerialNumberAsync(serialNumber);
        if (existingDevice is not null)
            throw new ValidationException("Device", "Device already registered.");

        var device = new Device
        {
            PatientId = patient.Id,
            SerialNumber = serialNumber,
            IsActive = true,
            RegisteredAt = DateTime.UtcNow
        };

        await _deviceRepo.CreateAsync(device);

        patient.DeviceSerialNumber = serialNumber;
        if (patient.WearableCode == code)
            patient.WearableCodeUsedAt = DateTime.UtcNow;
        else
            patient.CodeUsedAt = DateTime.UtcNow;
        await _patientRepo.UpdateAsync(patient);

        return new LinkDeviceResponse
        {
            Linked = true,
            SerialNumber = serialNumber,
            PatientId = patient.Id
        };
    }

    private static (DateTime? ExpiresAt, DateTime? UsedAt) GetCodeState(Patient patient, string code)
    {
        return patient.WearableCode == code
            ? (patient.WearableCodeExpiresAt, patient.WearableCodeUsedAt)
            : (patient.CodeExpiresAt, patient.CodeUsedAt);
    }
}
