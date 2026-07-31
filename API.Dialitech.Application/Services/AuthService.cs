using API.Dialitech.Application.Common.Exceptions;
using API.Dialitech.Application.DTOs;
using API.Dialitech.Application.Interfaces;
using API.Dialitech.Domain.Entities;
using API.Dialitech.Domain.Enums;
using API.Dialitech.Domain.Interfaces;

namespace API.Dialitech.Application.Services;

public class AuthService : IAuthService
{
    private readonly ICaregiverRepository _caregiverRepo;
    private readonly IPatientRepository _patientRepo;
    private readonly IDeviceRepository _deviceRepo;
    private readonly IAlertRepository _alertRepo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthService(
        ICaregiverRepository caregiverRepo,
        IPatientRepository patientRepo,
        IDeviceRepository deviceRepo,
        IAlertRepository alertRepo,
        IPasswordHasher passwordHasher,
        ITokenService tokenService)
    {
        _caregiverRepo = caregiverRepo;
        _patientRepo = patientRepo;
        _deviceRepo = deviceRepo;
        _alertRepo = alertRepo;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existing = await _caregiverRepo.GetByEmailAsync(request.Email);
        if (existing is not null)
            throw new ValidationException("Email", "Email already registered.");

        if (!Enum.TryParse<Plan>(request.Plan, true, out var plan))
            plan = Plan.Standard;

        var caregiver = new Caregiver
        {
            Name = request.Name,
            Lastname = request.Lastname,
            Phone = request.Phone,
            ImageUrl = request.ImageUrl,
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            Plan = plan
        };

        await _caregiverRepo.CreateAsync(caregiver);

        var token = _tokenService.GenerateToken(caregiver);

        return new AuthResponse
        {
            Token = token,
            Caregiver = MapToDto(caregiver)
        };
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var caregiver = await _caregiverRepo.GetByEmailAsync(request.Email);
        if (caregiver is null)
            return null;

        if (!_passwordHasher.Verify(request.Password, caregiver.PasswordHash))
            return null;

        var token = _tokenService.GenerateToken(caregiver);

        return new AuthResponse
        {
            Token = token,
            Caregiver = MapToDto(caregiver)
        };
    }

    public async Task<CaregiverDto?> GetByIdAsync(string id)
    {
        var caregiver = await _caregiverRepo.GetByIdAsync(id);
        return caregiver is null ? null : MapToDto(caregiver);
    }

    public async Task<CaregiverDto> UpdateProfileAsync(string caregiverId, UpdateProfileRequest request)
    {
        var caregiver = await _caregiverRepo.GetByIdAsync(caregiverId)
            ?? throw new KeyNotFoundException("Caregiver not found.");

        caregiver.Name = request.Name;
        caregiver.Lastname = request.Lastname;
        caregiver.Phone = request.Phone;
        caregiver.ImageUrl = request.ImageUrl;

        await _caregiverRepo.UpdateAsync(caregiver);

        return MapToDto(caregiver);
    }

    public async Task DeleteAccountAsync(string caregiverId)
    {
        var caregiver = await _caregiverRepo.GetByIdAsync(caregiverId)
            ?? throw new KeyNotFoundException("Caregiver not found.");

        var patients = await _patientRepo.GetByCaregiverIdAsync(caregiverId);
        foreach (var patient in patients)
        {
            await _deviceRepo.DeleteByPatientIdAsync(patient.Id);
            await _alertRepo.DeleteByPatientIdAsync(patient.Id);
            await _patientRepo.DeleteAsync(patient.Id);
        }

        await _caregiverRepo.DeleteAsync(caregiverId);
    }

    private static CaregiverDto MapToDto(Caregiver c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Lastname = c.Lastname,
        Phone = c.Phone,
        ImageUrl = c.ImageUrl,
        Email = c.Email,
        Plan = c.Plan.ToString()
    };
}
