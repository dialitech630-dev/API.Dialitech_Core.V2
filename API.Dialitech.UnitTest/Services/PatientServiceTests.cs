using API.Dialitech.Application.Common.Exceptions;
using API.Dialitech.Application.DTOs;
using API.Dialitech.Application.Services;
using API.Dialitech.Domain.Entities;
using API.Dialitech.Domain.Enums;
using API.Dialitech.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace API.Dialitech.UnitTest.Services;

public class PatientServiceTests
{
    private readonly Mock<IPatientRepository> _patientRepoMock;
    private readonly Mock<ICaregiverRepository> _caregiverRepoMock;
    private readonly PatientService _service;

    public PatientServiceTests()
    {
        _patientRepoMock = new Mock<IPatientRepository>();
        _caregiverRepoMock = new Mock<ICaregiverRepository>();
        _service = new PatientService(_patientRepoMock.Object, _caregiverRepoMock.Object);
    }

    [Fact]
    public async Task CreateAsync_WithinPlanLimit_CreatesPatient()
    {
        var caregiverId = "cg1";
        _caregiverRepoMock.Setup(r => r.GetByIdAsync(caregiverId))
            .ReturnsAsync(new Caregiver { Id = caregiverId, Plan = Plan.Standard });
        _patientRepoMock.Setup(r => r.CountByCaregiverIdAsync(caregiverId))
            .ReturnsAsync(0);

        var request = new CreatePatientRequest
        {
            Name = "Patient1",
            Age = 30,
            Gender = "Male"
        };

        var result = await _service.CreateAsync(caregiverId, request);

        result.Should().NotBeNull();
        result.Name.Should().Be("Patient1");
        _patientRepoMock.Verify(r => r.CreateAsync(It.IsAny<Patient>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ExceedsPlanLimit_ThrowsValidationException()
    {
        var caregiverId = "cg1";
        _caregiverRepoMock.Setup(r => r.GetByIdAsync(caregiverId))
            .ReturnsAsync(new Caregiver { Id = caregiverId, Plan = Plan.Standard });
        _patientRepoMock.Setup(r => r.CountByCaregiverIdAsync(caregiverId))
            .ReturnsAsync(1);

        var request = new CreatePatientRequest
        {
            Name = "Patient2",
            Age = 25,
            Gender = "Female"
        };

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.CreateAsync(caregiverId, request));
        _patientRepoMock.Verify(r => r.CreateAsync(It.IsAny<Patient>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_PremiumPlan_AllowsTenPatients()
    {
        var caregiverId = "cg1";
        _caregiverRepoMock.Setup(r => r.GetByIdAsync(caregiverId))
            .ReturnsAsync(new Caregiver { Id = caregiverId, Plan = Plan.Premium });
        _patientRepoMock.Setup(r => r.CountByCaregiverIdAsync(caregiverId))
            .ReturnsAsync(9);

        var request = new CreatePatientRequest { Name = "Patient10", Age = 40, Gender = "Male" };

        var result = await _service.CreateAsync(caregiverId, request);
        result.Should().NotBeNull();
        _patientRepoMock.Verify(r => r.CreateAsync(It.IsAny<Patient>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WrongCaregiver_ReturnsNull()
    {
        var patient = new Patient
        {
            Id = "p1",
            CaregiverId = "cg1",
            Name = "Test Patient"
        };
        _patientRepoMock.Setup(r => r.GetByIdAsync("p1"))
            .ReturnsAsync(patient);

        var result = await _service.GetByIdAsync("p1", "cg2");

        result.Should().BeNull();
    }
}
