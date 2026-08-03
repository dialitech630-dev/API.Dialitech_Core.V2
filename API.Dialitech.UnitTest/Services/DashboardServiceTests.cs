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
    private readonly Mock<IReadingRepository> _readingRepoMock;
    private readonly DashboardService _service;

    public DashboardServiceTests()
    {
        _patientRepoMock = new Mock<IPatientRepository>();
        _alertRepoMock = new Mock<IAlertRepository>();
        _readingRepoMock = new Mock<IReadingRepository>();
        _service = new DashboardService(
            _patientRepoMock.Object, _alertRepoMock.Object, _readingRepoMock.Object);
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

    [Fact]
    public async Task GetPatientReadingsAsync_ValidPatient_ReturnsOrderedReadings()
    {
        var patient = new Patient { Id = "p1", Name = "Test", CaregiverId = "cg1" };
        _patientRepoMock.Setup(r => r.GetByIdAsync("p1")).ReturnsAsync(patient);
        _readingRepoMock.Setup(r => r.GetByPatientIdAsync("p1", null, null, 500))
            .ReturnsAsync(new List<Reading>
            {
                new() { PatientId = "p1", HeartRate = 75, Oxygen = 98, Activity = 50, Timestamp = new DateTime(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc) },
                new() { PatientId = "p1", HeartRate = 72, Oxygen = 97, Activity = 40, Timestamp = new DateTime(2026, 8, 2, 11, 0, 0, DateTimeKind.Utc) }
            });

        var result = await _service.GetPatientReadingsAsync("p1", "cg1", null, null, 500);

        result.Should().NotBeNull();
        result!.Readings.Should().HaveCount(2);
        result.Readings[0].Timestamp.Should().Be(new DateTime(2026, 8, 2, 11, 0, 0, DateTimeKind.Utc));
        result.Readings[1].HeartRate.Should().Be(75);
    }

    [Fact]
    public async Task GetPatientReadingsAsync_OtherCaregiver_ReturnsNull()
    {
        var patient = new Patient { Id = "p1", Name = "Test", CaregiverId = "cg1" };
        _patientRepoMock.Setup(r => r.GetByIdAsync("p1")).ReturnsAsync(patient);

        var result = await _service.GetPatientReadingsAsync("p1", "cg2", null, null, 500);

        result.Should().BeNull();
        _readingRepoMock.Verify(r => r.GetByPatientIdAsync(It.IsAny<string>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetPatientReadingsAsync_NonExistingPatient_ReturnsNull()
    {
        _patientRepoMock.Setup(r => r.GetByIdAsync("p9")).ReturnsAsync((Patient?)null);

        var result = await _service.GetPatientReadingsAsync("p9", "cg1", null, null, 500);

        result.Should().BeNull();
    }
}
