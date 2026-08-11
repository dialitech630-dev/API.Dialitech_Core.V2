using System.Net;
using System.Text.Json;
using API.Dialitech.Application.DTOs;
using API.Dialitech.Application.Interfaces;
using API.Dialitech.Application.Services;
using API.Dialitech.Domain.Entities;
using API.Dialitech.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace API.Dialitech.UnitTest.Services;

public class HealthDataServiceTests : IDisposable
{
    private readonly Mock<IPatientRepository> _patientRepoMock;
    private readonly Mock<IAlertRepository> _alertRepoMock;
    private readonly Mock<IReadingRepository> _readingRepoMock;
    private readonly Mock<INotificationService> _notificationMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<HealthDataService>> _loggerMock;
    private readonly HealthDataService _service;
    private readonly HttpClient _mlHttpClient;

    public HealthDataServiceTests()
    {
        _patientRepoMock = new Mock<IPatientRepository>();
        _alertRepoMock = new Mock<IAlertRepository>();
        _readingRepoMock = new Mock<IReadingRepository>();
        _notificationMock = new Mock<INotificationService>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<HealthDataService>>();

        _mlHttpClient = new HttpClient { BaseAddress = new Uri("http://localhost:9999") };

        // Default: ML service not available (tests verify rule-based alerts only)
        _httpClientFactoryMock
            .Setup(f => f.CreateClient("MlService"))
            .Returns(_mlHttpClient);

        _service = new HealthDataService(
            _patientRepoMock.Object,
            _alertRepoMock.Object,
            _readingRepoMock.Object,
            _notificationMock.Object,
            _httpClientFactoryMock.Object,
            _loggerMock.Object);
    }

    public void Dispose()
    {
        _mlHttpClient.Dispose();
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
    public async Task GetPatientInfo_ExistingPatient_ReturnsInfo()
    {
        var patient = new Patient
        {
            Id = "p1",
            Code = "CODE1",
            Name = "Test Patient",
            DeviceSerialNumber = "SN12345",
            LastHeartRate = 75,
            LastOxygen = 98,
            LastActivity = 50,
            LastReadingAt = DateTime.UtcNow
        };
        _patientRepoMock.Setup(r => r.GetByCodeAsync("CODE1")).ReturnsAsync(patient);

        var result = await _service.GetPatientInfoAsync("CODE1");

        result.Should().NotBeNull();
        result.PatientCode.Should().Be("CODE1");
        result.Name.Should().Be("Test Patient");
        result.DeviceSerialNumber.Should().Be("SN12345");
        result.LastHeartRate.Should().Be(75);
        result.LastOxygen.Should().Be(98);
        result.LastActivity.Should().Be(50);
    }

    [Fact]
    public async Task GetPatientInfo_NonExistingPatient_ThrowsNotFound()
    {
        _patientRepoMock.Setup(r => r.GetByCodeAsync("INVALID")).ReturnsAsync((Patient?)null);

        await Assert.ThrowsAsync<API.Dialitech.Application.Common.Exceptions.NotFoundException>(
            () => _service.GetPatientInfoAsync("INVALID"));
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

    [Fact]
    public async Task ProcessBatch_InsertsOneReadingPerDataPoint()
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

        await _service.ProcessBatchAsync(request);

        _readingRepoMock.Verify(r => r.AddManyAsync(It.Is<IEnumerable<Reading>>(readings =>
            readings.Count() == 3)), Times.Once);
        _readingRepoMock.Verify(r => r.AddManyAsync(It.Is<IEnumerable<Reading>>(readings =>
            readings.All(rd =>
                rd.PatientId == "p1" &&
                rd.CaregiverId == "cg1" &&
                rd.Timestamp >= now.AddSeconds(-10) &&
                rd.Timestamp <= now))), Times.Once);
    }

    [Fact]
    public async Task ProcessBatch_AlertWithDeviceToken_SendsNotificationOnce()
    {
        var patient = new Patient
        {
            Id = "p1",
            CaregiverId = "cg1",
            Code = "CODE1",
            DeviceToken = "fcm-token-abc"
        };
        _patientRepoMock.Setup(r => r.GetByCodeAsync("CODE1")).ReturnsAsync(patient);

        var request = new BatchRequest
        {
            PatientCode = "CODE1",
            Data =
            [
                new BatchDataPoint { HeartRate = 140, Oxygen = 96, Activity = 80, Timestamp = DateTime.UtcNow },
                new BatchDataPoint { HeartRate = 130, Oxygen = 95, Activity = 70, Timestamp = DateTime.UtcNow }
            ]
        };

        await _service.ProcessBatchAsync(request);

        _notificationMock.Verify(n => n.SendHealthAlertAsync(
            "fcm-token-abc", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ProcessBatch_AlertWithoutDeviceToken_DoesNotSendNotification()
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

        await _service.ProcessBatchAsync(request);

        _notificationMock.Verify(
            n => n.SendHealthAlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessBatch_NotificationThrows_StillProcessesBatch()
    {
        var patient = new Patient
        {
            Id = "p1",
            CaregiverId = "cg1",
            Code = "CODE1",
            DeviceToken = "fcm-token-abc"
        };
        _patientRepoMock.Setup(r => r.GetByCodeAsync("CODE1")).ReturnsAsync(patient);
        _notificationMock.Setup(n => n.SendHealthAlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("firebase unavailable"));

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
        _readingRepoMock.Verify(r => r.AddManyAsync(It.IsAny<IEnumerable<Reading>>()), Times.Once);
        _patientRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Patient>()), Times.Once);
    }

    [Fact]
    public async Task RegisterDeviceToken_ExistingPatient_UpdatesToken()
    {
        var patient = new Patient { Id = "p1", CaregiverId = "cg1", Code = "CODE1" };
        _patientRepoMock.Setup(r => r.GetByCodeAsync("CODE1")).ReturnsAsync(patient);

        await _service.RegisterDeviceTokenAsync("CODE1", "fcm-token-xyz");

        _patientRepoMock.Verify(r => r.UpdateAsync(It.Is<Patient>(p =>
            p.DeviceToken == "fcm-token-xyz" &&
            p.DeviceTokenUpdatedAt.HasValue)), Times.Once);
    }

    [Fact]
    public async Task RegisterDeviceToken_NonExistingPatient_ThrowsNotFound()
    {
        _patientRepoMock.Setup(r => r.GetByCodeAsync("INVALID")).ReturnsAsync((Patient?)null);

        await Assert.ThrowsAsync<API.Dialitech.Application.Common.Exceptions.NotFoundException>(
            () => _service.RegisterDeviceTokenAsync("INVALID", "fcm-token"));
    }

    [Fact]
    public async Task RegisterDeviceToken_EmptyToken_ThrowsValidation()
    {
        await Assert.ThrowsAsync<API.Dialitech.Application.Common.Exceptions.ValidationException>(
            () => _service.RegisterDeviceTokenAsync("CODE1", "   "));
    }

    [Fact]
    public async Task GetPatientInfo_WithDeviceToken_HasDeviceTokenIsTrue()
    {
        var patient = new Patient
        {
            Id = "p1",
            Code = "CODE1",
            Name = "Test Patient",
            DeviceToken = "fcm-token-abc"
        };
        _patientRepoMock.Setup(r => r.GetByCodeAsync("CODE1")).ReturnsAsync(patient);

        var result = await _service.GetPatientInfoAsync("CODE1");

        result.HasDeviceToken.Should().BeTrue();
    }
}
