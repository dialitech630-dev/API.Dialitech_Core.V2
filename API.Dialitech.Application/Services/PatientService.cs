using API.Dialitech.Application.Common.Exceptions;
using API.Dialitech.Application.DTOs;
using API.Dialitech.Application.Interfaces;
using API.Dialitech.Domain.Entities;
using API.Dialitech.Domain.Enums;
using API.Dialitech.Domain.Interfaces;

namespace API.Dialitech.Application.Services;

public class PatientService : IPatientService
{
    private readonly IPatientRepository _patientRepo;
    private readonly ICaregiverRepository _caregiverRepo;

    public PatientService(IPatientRepository patientRepo, ICaregiverRepository caregiverRepo)
    {
        _patientRepo = patientRepo;
        _caregiverRepo = caregiverRepo;
    }

    public async Task<IEnumerable<PatientDto>> GetAllAsync(string caregiverId)
    {
        var patients = await _patientRepo.GetByCaregiverIdAsync(caregiverId);
        return patients.Select(MapToDto);
    }

    public async Task<PatientDto?> GetByIdAsync(string id, string caregiverId)
    {
        var patient = await _patientRepo.GetByIdAsync(id);
        if (patient is null || patient.CaregiverId != caregiverId)
            return null;

        return MapToDto(patient);
    }

    public async Task<PatientDto> CreateAsync(string caregiverId, CreatePatientRequest request)
    {
        var caregiver = await _caregiverRepo.GetByIdAsync(caregiverId)
            ??             throw new NotFoundException("Caregiver", caregiverId);

        var patientCount = await _patientRepo.CountByCaregiverIdAsync(caregiverId);
        var planLimit = (int)caregiver.Plan;

        if (patientCount >= planLimit)
            throw new ValidationException("Plan",
                $"Plan {caregiver.Plan} allows maximum {planLimit} patients.");

        var patient = new Patient
        {
            CaregiverId = caregiverId,
            Name = request.Name,
            Age = request.Age,
            Gender = request.Gender,
            Notes = request.Notes
        };

        await _patientRepo.CreateAsync(patient);
        return MapToDto(patient);
    }

    public async Task DeleteAsync(string id, string caregiverId)
    {
        var patient = await _patientRepo.GetByIdAsync(id);
        if (patient is null)
            return;

        if (patient.CaregiverId != caregiverId)
            throw new NotFoundException("Patient", id);

        await _patientRepo.DeleteAsync(id);
    }

    private static PatientDto MapToDto(Patient p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Age = p.Age,
        Gender = p.Gender,
        Notes = p.Notes,
        DeviceSerialNumber = p.DeviceSerialNumber,
        LastHeartRate = p.LastHeartRate,
        LastOxygen = p.LastOxygen,
        LastActivity = p.LastActivity,
        LastReadingAt = p.LastReadingAt,
        CreatedAt = p.CreatedAt
    };
}
