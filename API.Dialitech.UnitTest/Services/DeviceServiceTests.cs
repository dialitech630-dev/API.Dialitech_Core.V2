using API.Dialitech.Application.Common.Exceptions;
using API.Dialitech.Application.DTOs;
using API.Dialitech.Application.Services;
using API.Dialitech.Domain.Entities;
using API.Dialitech.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace API.Dialitech.UnitTest.Services;

public class DeviceServiceTests
{
    private readonly Mock<IPatientRepository> _patientRepoMock;
    private readonly Mock<IDeviceRepository> _deviceRepoMock;
    private readonly DeviceService _service;

    public DeviceServiceTests()
    {
        _patientRepoMock = new Mock<IPatientRepository>();
        _deviceRepoMock = new Mock<IDeviceRepository>();
        _service = new DeviceService(_patientRepoMock.Object, _deviceRepoMock.Object);
    }

    [Fact]
    public async Task GenerateCode_ReturnsValidCode()
    {
        var patient = new Patient { Id = "p1", CaregiverId = "cg1" };
        _patientRepoMock.Setup(r => r.GetByIdAsync("p1")).ReturnsAsync(patient);

        var result = await _service.GenerateCodeAsync("p1", "cg1");

        result.Should().NotBeNull();
        result.Code.Should().HaveLength(6);
        result.ExpiresInSeconds.Should().Be(300);
    }

    [Fact]
    public async Task ValidateCode_ValidCode_ReturnsValid()
    {
        var patient = new Patient
        {
            Id = "p1",
            Name = "Test",
            Code = "123456",
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(4)
        };
        _patientRepoMock.Setup(r => r.GetByCodeAsync("123456")).ReturnsAsync(patient);

        var result = await _service.ValidateCodeAsync("123456");

        result.IsValid.Should().BeTrue();
        result.PatientId.Should().Be("p1");
    }

    [Fact]
    public async Task ValidateCode_ExpiredCode_ReturnsInvalid()
    {
        var patient = new Patient
        {
            Code = "123456",
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        };
        _patientRepoMock.Setup(r => r.GetByCodeAsync("123456")).ReturnsAsync(patient);

        var result = await _service.ValidateCodeAsync("123456");

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task LinkDevice_ValidCode_LinksSuccessfully()
    {
        var patient = new Patient
        {
            Id = "p1",
            Name = "Test",
            CaregiverId = "cg1",
            Code = "123456",
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(4)
        };
        _patientRepoMock.Setup(r => r.GetByCodeAsync("123456")).ReturnsAsync(patient);
        _deviceRepoMock.Setup(r => r.GetBySerialNumberAsync("SN001"))
            .ReturnsAsync((Device?)null);

        var result = await _service.LinkDeviceAsync("123456", "SN001");

        result.Linked.Should().BeTrue();
        result.SerialNumber.Should().Be("SN001");
        _deviceRepoMock.Verify(r => r.CreateAsync(It.IsAny<Device>()), Times.Once);
        _patientRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Patient>()), Times.Once);
    }

    [Fact]
    public async Task GenerateCode_PatientNotFound_ThrowsNotFoundException()
    {
        _patientRepoMock.Setup(r => r.GetByIdAsync("nonexistent"))
            .ReturnsAsync((Patient?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.GenerateCodeAsync("nonexistent", "cg1"));
    }

    [Fact]
    public async Task GenerateCode_WrongCaregiver_ThrowsNotFoundException()
    {
        var patient = new Patient { Id = "p1", CaregiverId = "cg1" };
        _patientRepoMock.Setup(r => r.GetByIdAsync("p1")).ReturnsAsync(patient);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.GenerateCodeAsync("p1", "cg2"));
    }

    [Fact]
    public async Task LinkDevice_ExpiredCode_ThrowsValidationException()
    {
        var patient = new Patient
        {
            Id = "p1",
            Code = "123456",
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        };
        _patientRepoMock.Setup(r => r.GetByCodeAsync("123456")).ReturnsAsync(patient);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.LinkDeviceAsync("123456", "SN001"));
    }

    [Fact]
    public async Task LinkDevice_DeviceAlreadyRegistered_ThrowsValidationException()
    {
        var patient = new Patient
        {
            Id = "p1",
            Code = "123456",
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(4)
        };
        var existingDevice = new Device { SerialNumber = "SN001", PatientId = "p2" };
        _patientRepoMock.Setup(r => r.GetByCodeAsync("123456")).ReturnsAsync(patient);
        _deviceRepoMock.Setup(r => r.GetBySerialNumberAsync("SN001")).ReturnsAsync(existingDevice);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.LinkDeviceAsync("123456", "SN001"));
    }

    [Fact]
    public async Task ValidateCode_InvalidCode_ReturnsInvalid()
    {
        _patientRepoMock.Setup(r => r.GetByCodeAsync("INVALID"))
            .ReturnsAsync((Patient?)null);

        var result = await _service.ValidateCodeAsync("INVALID");

        result.IsValid.Should().BeFalse();
    }
}
