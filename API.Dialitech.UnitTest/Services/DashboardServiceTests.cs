using API.Dialitech.Application.Services;
using API.Dialitech.Domain.Entities;
using API.Dialitech.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace API.Dialitech.UnitTest.Services;

public class DashboardServiceTests
{
    private readonly Mock<IPatientRepository> _patientRepoMock;
    private readonly Mock<IAlertRepository> _alertRepoMock;
    private readonly DashboardService _service;

    public DashboardServiceTests()
    {
        _patientRepoMock = new Mock<IPatientRepository>();
        _alertRepoMock = new Mock<IAlertRepository>();
        _service = new DashboardService(_patientRepoMock.Object, _alertRepoMock.Object);
    }

    [Fact]
    public async Task GetSummaryAsync_MapsPatientsCorrectly()
    {
        var patients = new List<Patient>
        {
            new() { Id = "p1", Name = "Patient1", CaregiverId = "cg1", DeviceSerialNumber = "SN001", LastHeartRate = 72, LastOxygen = 98 },
            new() { Id = "p2", Name = "Patient2", CaregiverId = "cg1" }
        };
        _patientRepoMock.Setup(r => r.GetByCaregiverIdAsync("cg1")).ReturnsAsync(patients);
        _alertRepoMock.Setup(r => r.GetByCaregiverIdAsync("cg1"))
            .ReturnsAsync(new List<Alert>());

        var result = await _service.GetSummaryAsync("cg1");

        result.TotalPatients.Should().Be(2);
        result.PatientsWithDevice.Should().Be(1);
        result.Patients.Should().HaveCount(2);
        result.Patients[0].LastHeartRate.Should().Be(72);
        result.Patients[1].HasDevice.Should().BeFalse();
    }

    [Fact]
    public async Task GetPatientStatusAsync_ValidPatient_ReturnsStatus()
    {
        var patient = new Patient
        {
            Id = "p1",
            Name = "Test",
            CaregiverId = "cg1",
            LastHeartRate = 75,
            LastOxygen = 97
        };
        _patientRepoMock.Setup(r => r.GetByIdAsync("p1")).ReturnsAsync(patient);
        _alertRepoMock.Setup(r => r.GetByPatientIdAsync("p1"))
            .ReturnsAsync(new List<Alert>());

        var result = await _service.GetPatientStatusAsync("p1", "cg1");

        result.Should().NotBeNull();
        result!.LastHeartRate.Should().Be(75);
        result.LastOxygen.Should().Be(97);
    }
}
