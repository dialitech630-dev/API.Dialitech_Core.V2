using API.Dialitech.Application.Common.Exceptions;
using API.Dialitech.Application.Services;
using API.Dialitech.Domain.Entities;
using API.Dialitech.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace API.Dialitech.UnitTest.Services;

public class AlertServiceTests
{
    private readonly Mock<IAlertRepository> _alertRepoMock;
    private readonly Mock<IPatientRepository> _patientRepoMock;
    private readonly AlertService _service;

    public AlertServiceTests()
    {
        _alertRepoMock = new Mock<IAlertRepository>();
        _patientRepoMock = new Mock<IPatientRepository>();
        _service = new AlertService(_alertRepoMock.Object, _patientRepoMock.Object);
    }

    [Fact]
    public async Task GetByPatientAsync_ValidPatient_ReturnsAlerts()
    {
        var patient = new Patient { Id = "p1", CaregiverId = "cg1", Name = "Test" };
        _patientRepoMock.Setup(r => r.GetByIdAsync("p1")).ReturnsAsync(patient);

        var alerts = new List<Alert>
        {
            new() { Id = "a1", PatientId = "p1", Type = "HeartRateHigh", Message = "HR high", Severity = 2, CreatedAt = DateTime.UtcNow }
        };
        _alertRepoMock.Setup(r => r.GetByPatientIdAsync("p1")).ReturnsAsync(alerts);

        var result = await _service.GetByPatientAsync("p1", "cg1");

        result.Should().HaveCount(1);
        result.First().Type.Should().Be("HeartRateHigh");
        result.First().PatientName.Should().Be("Test");
    }

    [Fact]
    public async Task GetByCaregiverAsync_ReturnsAlerts()
    {
        var patients = new List<Patient>
        {
            new() { Id = "p1", Name = "Patient1", CaregiverId = "cg1" }
        };
        _patientRepoMock.Setup(r => r.GetByCaregiverIdAsync("cg1")).ReturnsAsync(patients);

        var alerts = new List<Alert>
        {
            new() { Id = "a1", PatientId = "p1", Type = "OxygenLow", Message = "O2 low", Severity = 2 }
        };
        _alertRepoMock.Setup(r => r.GetByCaregiverIdAsync("cg1")).ReturnsAsync(alerts);

        var result = await _service.GetByCaregiverAsync("cg1");

        result.Should().HaveCount(1);
        result.First().PatientName.Should().Be("Patient1");
    }

    [Fact]
    public async Task GetByPatientAsync_PatientNotFound_ReturnsEmpty()
    {
        _patientRepoMock.Setup(r => r.GetByIdAsync("nonexistent"))
            .ReturnsAsync((Patient?)null);

        var result = await _service.GetByPatientAsync("nonexistent", "cg1");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByPatientAsync_WrongCaregiver_ReturnsEmpty()
    {
        var patient = new Patient { Id = "p1", CaregiverId = "cg1" };
        _patientRepoMock.Setup(r => r.GetByIdAsync("p1")).ReturnsAsync(patient);

        var result = await _service.GetByPatientAsync("p1", "cg2");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAsync_AlertNotFound_ThrowsNotFoundException()
    {
        var caregiverAlerts = new List<Alert>();
        _alertRepoMock.Setup(r => r.GetByPatientIdAsync("alert1"))
            .ReturnsAsync(new List<Alert>());
        _alertRepoMock.Setup(r => r.GetByCaregiverIdAsync("cg1"))
            .ReturnsAsync(caregiverAlerts);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.DeleteAsync("alert1", "cg1"));
    }

    [Fact]
    public async Task DeleteAsync_HappyPath_DeletesAlert()
    {
        var alert = new Alert { Id = "a1", PatientId = "p1", CaregiverId = "cg1" };
        var caregiverAlerts = new List<Alert> { alert };
        _alertRepoMock.Setup(r => r.GetByPatientIdAsync("a1"))
            .ReturnsAsync(new List<Alert> { alert });
        _alertRepoMock.Setup(r => r.GetByCaregiverIdAsync("cg1"))
            .ReturnsAsync(caregiverAlerts);

        await _service.DeleteAsync("a1", "cg1");

        _alertRepoMock.Verify(r => r.DeleteAsync("a1"), Times.Once);
    }

    [Fact]
    public async Task GetByCaregiverAsync_NoAlerts_ReturnsEmpty()
    {
        _patientRepoMock.Setup(r => r.GetByCaregiverIdAsync("cg1"))
            .ReturnsAsync(new List<Patient>());
        _alertRepoMock.Setup(r => r.GetByCaregiverIdAsync("cg1"))
            .ReturnsAsync(new List<Alert>());

        var result = await _service.GetByCaregiverAsync("cg1");

        result.Should().BeEmpty();
    }
}
