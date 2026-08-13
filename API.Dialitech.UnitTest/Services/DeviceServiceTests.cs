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
    public async Task GenerateWearableCode_SetsWearableCodeField()
    {
        var patient = new Patient { Id = "p1", CaregiverId = "cg1" };
        _patientRepoMock.Setup(r => r.GetByIdAsync("p1")).ReturnsAsync(patient);

        var result = await _service.GenerateWearableCodeAsync("p1", "cg1");

        result.Code.Should().HaveLength(6);
        result.ExpiresInSeconds.Should().Be(300);
        patient.WearableCode.Should().Be(result.Code);
        patient.WearableCodeExpiresAt.Should().NotBeNull();
        patient.Code.Should().BeNull();
        _patientRepoMock.Verify(r => r.UpdateAsync(patient), Times.Once);
    }

    [Fact]
    public async Task GenerateWearableCode_OtherCaregiver_ThrowsValidationException()
    {
        var patient = new Patient { Id = "p1", CaregiverId = "cg1" };
        _patientRepoMock.Setup(r => r.GetByIdAsync("p1")).ReturnsAsync(patient);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.GenerateWearableCodeAsync("p1", "cg2"));
    }

    [Fact]
    public async Task GenerateWearableCode_NonExistingPatient_ThrowsNotFoundException()
    {
        _patientRepoMock.Setup(r => r.GetByIdAsync("p9")).ReturnsAsync((Patient?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.GenerateWearableCodeAsync("p9", "cg1"));
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
    public async Task ValidateCode_WearableCode_Valid_WhenLegacyExpired()
    {
        var patient = new Patient
        {
            Id = "p1",
            Name = "Test",
            Code = "111",
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(-5),
            WearableCode = "654321",
            WearableCodeExpiresAt = DateTime.UtcNow.AddMinutes(4)
        };
        _patientRepoMock.Setup(r => r.GetByCodeAsync("654321")).ReturnsAsync(patient);

        var result = await _service.ValidateCodeAsync("654321");

        result.IsValid.Should().BeTrue();
        result.PatientId.Should().Be("p1");
    }

    [Fact]
    public async Task ValidateCode_WearableCode_Expired_ReturnsInvalid_EvenWhenLegacyFresh()
    {
        var patient = new Patient
        {
            Code = "111",
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(5),
            WearableCode = "654321",
            WearableCodeExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        };
        _patientRepoMock.Setup(r => r.GetByCodeAsync("654321")).ReturnsAsync(patient);

        var result = await _service.ValidateCodeAsync("654321");

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateCode_NullExpiry_ReturnsInvalid()
    {
        var patient = new Patient
        {
            Code = "123456",
            CodeExpiresAt = null
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
    public async Task LinkDevice_WearableCode_Valid_WhenLegacyExpired()
    {
        var patient = new Patient
        {
            Id = "p1",
            Name = "Test",
            CaregiverId = "cg1",
            Code = "111111",
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(-5),
            WearableCode = "654321",
            WearableCodeExpiresAt = DateTime.UtcNow.AddMinutes(4)
        };
        _patientRepoMock.Setup(r => r.GetByCodeAsync("654321")).ReturnsAsync(patient);
        _deviceRepoMock.Setup(r => r.GetBySerialNumberAsync("SN002"))
            .ReturnsAsync((Device?)null);

        var result = await _service.LinkDeviceAsync("654321", "SN002");

        result.Linked.Should().BeTrue();
        result.SerialNumber.Should().Be("SN002");
        _patientRepoMock.Verify(r => r.UpdateAsync(It.Is<Patient>(p =>
            p.DeviceSerialNumber == "SN002")), Times.Once);
    }

    [Fact]
    public async Task LinkDevice_WearableCode_Expired_Throws_EvenWhenLegacyFresh()
    {
        var patient = new Patient
        {
            Id = "p1",
            Name = "Test",
            CaregiverId = "cg1",
            Code = "111111",
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(5),
            WearableCode = "654321",
            WearableCodeExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        };
        _patientRepoMock.Setup(r => r.GetByCodeAsync("654321")).ReturnsAsync(patient);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.LinkDeviceAsync("654321", "SN002"));

        _deviceRepoMock.Verify(r => r.CreateAsync(It.IsAny<Device>()), Times.Never);
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

    [Fact]
    public async Task ValidateCode_UsedCode_ReturnsInvalid()
    {
        var patient = new Patient
        {
            Id = "p1",
            Name = "Test",
            Code = "123456",
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(4),
            CodeUsedAt = DateTime.UtcNow.AddMinutes(-1)
        };
        _patientRepoMock.Setup(r => r.GetByCodeAsync("123456")).ReturnsAsync(patient);

        var result = await _service.ValidateCodeAsync("123456");

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateCode_WearableCode_Used_ReturnsInvalid_EvenWhenLegacyFresh()
    {
        var patient = new Patient
        {
            Id = "p1",
            Name = "Test",
            Code = "111111",
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(5),
            WearableCode = "654321",
            WearableCodeExpiresAt = DateTime.UtcNow.AddMinutes(4),
            WearableCodeUsedAt = DateTime.UtcNow.AddMinutes(-1)
        };
        _patientRepoMock.Setup(r => r.GetByCodeAsync("654321")).ReturnsAsync(patient);

        var result = await _service.ValidateCodeAsync("654321");

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task LinkDevice_UsedCode_ThrowsValidationException()
    {
        var patient = new Patient
        {
            Id = "p1",
            Name = "Test",
            CaregiverId = "cg1",
            Code = "123456",
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(4),
            CodeUsedAt = DateTime.UtcNow.AddMinutes(-1)
        };
        _patientRepoMock.Setup(r => r.GetByCodeAsync("123456")).ReturnsAsync(patient);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.LinkDeviceAsync("123456", "SN001"));

        _deviceRepoMock.Verify(r => r.CreateAsync(It.IsAny<Device>()), Times.Never);
        _patientRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Patient>()), Times.Never);
    }

    [Fact]
    public async Task LinkDevice_UsedCode_SameSerial_ReturnsLinked_Idempotent()
    {
        var patient = new Patient
        {
            Id = "p1",
            Name = "Test",
            CaregiverId = "cg1",
            Code = "123456",
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(4),
            CodeUsedAt = DateTime.UtcNow.AddMinutes(-1),
            DeviceSerialNumber = "SN001"
        };
        _patientRepoMock.Setup(r => r.GetByCodeAsync("123456")).ReturnsAsync(patient);

        var result = await _service.LinkDeviceAsync("123456", "SN001");

        result.Linked.Should().BeTrue();
        result.SerialNumber.Should().Be("SN001");
        _deviceRepoMock.Verify(r => r.CreateAsync(It.IsAny<Device>()), Times.Never);
        _patientRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Patient>()), Times.Never);
    }

    [Fact]
    public async Task LinkDevice_ValidCode_MarksCodeAsUsed()
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
        _patientRepoMock.Verify(r => r.UpdateAsync(It.Is<Patient>(p =>
            p.CodeUsedAt.HasValue)), Times.Once);
    }

    [Fact]
    public async Task LinkDevice_WearableCode_Valid_MarksWearableCodeAsUsed()
    {
        var patient = new Patient
        {
            Id = "p1",
            Name = "Test",
            CaregiverId = "cg1",
            WearableCode = "654321",
            WearableCodeExpiresAt = DateTime.UtcNow.AddMinutes(4)
        };
        _patientRepoMock.Setup(r => r.GetByCodeAsync("654321")).ReturnsAsync(patient);
        _deviceRepoMock.Setup(r => r.GetBySerialNumberAsync("SN002"))
            .ReturnsAsync((Device?)null);

        var result = await _service.LinkDeviceAsync("654321", "SN002");

        result.Linked.Should().BeTrue();
        _patientRepoMock.Verify(r => r.UpdateAsync(It.Is<Patient>(p =>
            p.WearableCodeUsedAt.HasValue &&
            p.CodeUsedAt == null)), Times.Once);
    }

    [Fact]
    public async Task GenerateCode_ResetsUsedFlag()
    {
        var patient = new Patient
        {
            Id = "p1",
            CaregiverId = "cg1",
            Code = "999999",
            CodeExpiresAt = DateTime.UtcNow.AddMinutes(4),
            CodeUsedAt = DateTime.UtcNow.AddMinutes(-1)
        };
        _patientRepoMock.Setup(r => r.GetByIdAsync("p1")).ReturnsAsync(patient);

        var result = await _service.GenerateCodeAsync("p1", "cg1");

        patient.Code.Should().Be(result.Code);
        patient.CodeUsedAt.Should().BeNull();
    }

    [Fact]
    public async Task GenerateWearableCode_ResetsUsedFlag()
    {
        var patient = new Patient
        {
            Id = "p1",
            CaregiverId = "cg1",
            WearableCode = "999999",
            WearableCodeExpiresAt = DateTime.UtcNow.AddMinutes(4),
            WearableCodeUsedAt = DateTime.UtcNow.AddMinutes(-1)
        };
        _patientRepoMock.Setup(r => r.GetByIdAsync("p1")).ReturnsAsync(patient);

        var result = await _service.GenerateWearableCodeAsync("p1", "cg1");

        patient.WearableCode.Should().Be(result.Code);
        patient.WearableCodeUsedAt.Should().BeNull();
    }
}
