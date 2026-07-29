using API.Dialitech.Application.DTOs;
using API.Dialitech.Application.Services;
using API.Dialitech.Domain.Entities;
using API.Dialitech.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace API.Dialitech.UnitTest.Services;

public class HealthDataServiceTests
{
    private readonly Mock<IPatientRepository> _patientRepoMock;
    private readonly Mock<IAlertRepository> _alertRepoMock;
    private readonly HealthDataService _service;

    public HealthDataServiceTests()
    {
        _patientRepoMock = new Mock<IPatientRepository>();
        _alertRepoMock = new Mock<IAlertRepository>();
        _service = new HealthDataService(_patientRepoMock.Object, _alertRepoMock.Object);
    }

    [Fact]
    public async Task ProcessBatch_NormalValues_NoAlerts()
    {
        var patient = new Patient { Id = "p1", CaregiverId = "cg1", Code = "CODE1" };
        _patientRepoMock.Setup(r => r.GetByCodeAsync("CODE1")).ReturnsAsync(patient);

        var request = new BatchRequest
        {
            PatientCode = "CODE1",
            Data =
            [
                new BatchDataPoint { HeartRate = 75, Oxygen = 98, Activity = 50, Timestamp = DateTime.UtcNow }
            ]
        };

        var result = await _service.ProcessBatchAsync(request);

        result.Status.Should().Be("processed");
        result.AlertsTriggered.Should().Be(0);
        _alertRepoMock.Verify(r => r.CreateAsync(It.IsAny<Alert>()), Times.Never);
    }

    [Fact]
    public async Task ProcessBatch_HighHeartRate_TriggersAlert()
    {
        var patient = new Patient { Id = "p1", CaregiverId = "cg1", Code = "CODE1" };
        _patientRepoMock.Setup(r => r.GetByCodeAsync("CODE1")).ReturnsAsync(patient);

        var request = new BatchRequest
        {
            PatientCode = "CODE1",
            Data =
            [
                new BatchDataPoint { HeartRate = 130, Oxygen = 97, Activity = 80, Timestamp = DateTime.UtcNow }
            ]
        };

        var result = await _service.ProcessBatchAsync(request);

        result.AlertsTriggered.Should().Be(1);
        _alertRepoMock.Verify(r => r.CreateAsync(It.IsAny<Alert>()), Times.Once);
    }

    [Fact]
    public async Task ProcessBatch_LowOxygen_TriggersAlert()
    {
        var patient = new Patient { Id = "p1", CaregiverId = "cg1", Code = "CODE1" };
        _patientRepoMock.Setup(r => r.GetByCodeAsync("CODE1")).ReturnsAsync(patient);

        var request = new BatchRequest
        {
            PatientCode = "CODE1",
            Data =
            [
                new BatchDataPoint { HeartRate = 72, Oxygen = 85, Activity = 30, Timestamp = DateTime.UtcNow }
            ]
        };

        var result = await _service.ProcessBatchAsync(request);

        result.AlertsTriggered.Should().Be(1);
        _alertRepoMock.Verify(r => r.CreateAsync(It.IsAny<Alert>()), Times.Once);
    }

    [Fact]
    public async Task ProcessBatch_MultipleReadings_SavesLastState()
    {
        var patient = new Patient { Id = "p1", CaregiverId = "cg1", Code = "CODE1" };
        _patientRepoMock.Setup(r => r.GetByCodeAsync("CODE1")).ReturnsAsync(patient);

        var now = DateTime.UtcNow;
        var request = new BatchRequest
        {
            PatientCode = "CODE1",
            Data =
            [
                new BatchDataPoint { HeartRate = 70, Oxygen = 98, Activity = 20, Timestamp = now.AddSeconds(-10) },
                new BatchDataPoint { HeartRate = 72, Oxygen = 97, Activity = 25, Timestamp = now.AddSeconds(-5) },
                new BatchDataPoint { HeartRate = 75, Oxygen = 96, Activity = 30, Timestamp = now }
            ]
        };

        var result = await _service.ProcessBatchAsync(request);

        result.Status.Should().Be("processed");
        _patientRepoMock.Verify(r => r.UpdateAsync(It.Is<Patient>(p =>
            p.LastHeartRate == 75 &&
            p.LastOxygen == 96 &&
            p.LastActivity == 30)), Times.Once);
    }
}
