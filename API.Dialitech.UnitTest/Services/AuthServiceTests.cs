using API.Dialitech.Application.Common.Exceptions;
using API.Dialitech.Application.DTOs;
using API.Dialitech.Application.Interfaces;
using API.Dialitech.Application.Services;
using API.Dialitech.Domain.Entities;
using API.Dialitech.Domain.Enums;
using API.Dialitech.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace API.Dialitech.UnitTest.Services;

public class AuthServiceTests
{
    private readonly Mock<ICaregiverRepository> _caregiverRepoMock;
    private readonly Mock<IPatientRepository> _patientRepoMock;
    private readonly Mock<IDeviceRepository> _deviceRepoMock;
    private readonly Mock<IAlertRepository> _alertRepoMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _caregiverRepoMock = new Mock<ICaregiverRepository>();
        _patientRepoMock = new Mock<IPatientRepository>();
        _deviceRepoMock = new Mock<IDeviceRepository>();
        _alertRepoMock = new Mock<IAlertRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _tokenServiceMock = new Mock<ITokenService>();
        _service = new AuthService(
            _caregiverRepoMock.Object,
            _patientRepoMock.Object,
            _deviceRepoMock.Object,
            _alertRepoMock.Object,
            _passwordHasherMock.Object,
            _tokenServiceMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_ReturnsAuthResponse()
    {
        _caregiverRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((Caregiver?)null);
        _passwordHasherMock.Setup(p => p.Hash(It.IsAny<string>()))
            .Returns("hashed");
        _tokenServiceMock.Setup(t => t.GenerateToken(It.IsAny<Caregiver>()))
            .Returns("jwt-token");

        var request = new RegisterRequest
        {
            Name = "Test",
            Email = "test@test.com",
            Password = "pass123",
            Plan = "Standard"
        };

        var result = await _service.RegisterAsync(request);

        result.Should().NotBeNull();
        result.Token.Should().Be("jwt-token");
        result.Caregiver.Name.Should().Be("Test");
        result.Caregiver.Email.Should().Be("test@test.com");
        result.Caregiver.Plan.Should().Be("Standard");
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsValidationException()
    {
        _caregiverRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(new Caregiver { Email = "existing@test.com" });

        var request = new RegisterRequest
        {
            Name = "Test",
            Email = "existing@test.com",
            Password = "pass123",
            Plan = "Standard"
        };

        await Assert.ThrowsAsync<ValidationException>(() => _service.RegisterAsync(request));
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsAuthResponse()
    {
        var caregiver = new Caregiver
        {
            Id = "1",
            Name = "Test",
            Email = "test@test.com",
            PasswordHash = "hashed",
            Plan = Plan.Standard
        };

        _caregiverRepoMock.Setup(r => r.GetByEmailAsync("test@test.com"))
            .ReturnsAsync(caregiver);
        _passwordHasherMock.Setup(p => p.Verify("pass123", "hashed"))
            .Returns(true);
        _tokenServiceMock.Setup(t => t.GenerateToken(caregiver))
            .Returns("jwt-token");

        var result = await _service.LoginAsync(new LoginRequest
        {
            Email = "test@test.com",
            Password = "pass123"
        });

        result.Should().NotBeNull();
        result!.Token.Should().Be("jwt-token");
    }

    [Fact]
    public async Task LoginAsync_InvalidCredentials_ReturnsNull()
    {
        _caregiverRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((Caregiver?)null);

        var result = await _service.LoginAsync(new LoginRequest
        {
            Email = "wrong@test.com",
            Password = "wrong"
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsNull()
    {
        var caregiver = new Caregiver
        {
            Email = "test@test.com",
            PasswordHash = "hashed"
        };

        _caregiverRepoMock.Setup(r => r.GetByEmailAsync("test@test.com"))
            .ReturnsAsync(caregiver);
        _passwordHasherMock.Setup(p => p.Verify("wrong", "hashed"))
            .Returns(false);

        var result = await _service.LoginAsync(new LoginRequest
        {
            Email = "test@test.com",
            Password = "wrong"
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task ChangePasswordAsync_ValidCurrentPassword_UpdatesHash()
    {
        var caregiver = new Caregiver
        {
            Id = "1",
            Email = "test@test.com",
            PasswordHash = "old-hash"
        };

        _caregiverRepoMock.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(caregiver);
        _passwordHasherMock.Setup(p => p.Verify("old-pass", "old-hash")).Returns(true);
        _passwordHasherMock.Setup(p => p.Hash("new-pass")).Returns("new-hash");

        await _service.ChangePasswordAsync("1", new ChangePasswordRequest
        {
            CurrentPassword = "old-pass",
            NewPassword = "new-pass"
        });

        caregiver.PasswordHash.Should().Be("new-hash");
        _caregiverRepoMock.Verify(r => r.UpdateAsync(caregiver), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongCurrentPassword_ThrowsValidationException()
    {
        var caregiver = new Caregiver
        {
            Id = "1",
            Email = "test@test.com",
            PasswordHash = "old-hash"
        };

        _caregiverRepoMock.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(caregiver);
        _passwordHasherMock.Setup(p => p.Verify("wrong", "old-hash")).Returns(false);

        await Assert.ThrowsAsync<ValidationException>(() => _service.ChangePasswordAsync("1",
            new ChangePasswordRequest { CurrentPassword = "wrong", NewPassword = "new-pass" }));
    }

    [Fact]
    public async Task ForgotPasswordAsync_ExistingEmail_ReturnsSixDigitCode()
    {
        var caregiver = new Caregiver
        {
            Id = "1",
            Email = "test@test.com"
        };

        _caregiverRepoMock.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(caregiver);

        var code = await _service.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "test@test.com" });

        code.Should().MatchRegex("^\\d{6}$");
        caregiver.ResetCode.Should().Be(code);
        caregiver.ResetCodeExpiresAt.Should().NotBeNull();
        _caregiverRepoMock.Verify(r => r.UpdateAsync(caregiver), Times.Once);
    }

    [Fact]
    public async Task ForgotPasswordAsync_UnknownEmail_ThrowsNotFoundException()
    {
        _caregiverRepoMock.Setup(r => r.GetByEmailAsync("nobody@test.com"))
            .ReturnsAsync((Caregiver?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.ForgotPasswordAsync(new ForgotPasswordRequest { Email = "nobody@test.com" }));
    }

    [Fact]
    public async Task ResetPasswordAsync_ValidCode_UpdatesPassword()
    {
        var caregiver = new Caregiver
        {
            Id = "1",
            Email = "test@test.com",
            PasswordHash = "old-hash",
            ResetCode = "123456",
            ResetCodeExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

        _caregiverRepoMock.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(caregiver);
        _passwordHasherMock.Setup(p => p.Hash("new-pass")).Returns("new-hash");

        await _service.ResetPasswordAsync(new ResetPasswordRequest
        {
            Email = "test@test.com",
            Code = "123456",
            NewPassword = "new-pass"
        });

        caregiver.PasswordHash.Should().Be("new-hash");
        caregiver.ResetCode.Should().BeNull();
        caregiver.ResetCodeExpiresAt.Should().BeNull();
    }

    [Fact]
    public async Task ResetPasswordAsync_ExpiredCode_ThrowsValidationException()
    {
        var caregiver = new Caregiver
        {
            Id = "1",
            Email = "test@test.com",
            ResetCode = "123456",
            ResetCodeExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        };

        _caregiverRepoMock.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(caregiver);

        await Assert.ThrowsAsync<ValidationException>(() => _service.ResetPasswordAsync(
            new ResetPasswordRequest { Email = "test@test.com", Code = "123456", NewPassword = "new-pass" }));
    }

    [Fact]
    public async Task ResetPasswordAsync_WrongCode_ThrowsValidationException()
    {
        var caregiver = new Caregiver
        {
            Id = "1",
            Email = "test@test.com",
            ResetCode = "123456",
            ResetCodeExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

        _caregiverRepoMock.Setup(r => r.GetByEmailAsync("test@test.com")).ReturnsAsync(caregiver);

        await Assert.ThrowsAsync<ValidationException>(() => _service.ResetPasswordAsync(
            new ResetPasswordRequest { Email = "test@test.com", Code = "999999", NewPassword = "new-pass" }));
    }

    [Fact]
    public async Task ChangePlanAsync_ValidPlan_UpdatesPlan()
    {
        var caregiver = new Caregiver
        {
            Id = "1",
            Email = "test@test.com",
            Plan = Plan.Standard
        };

        _caregiverRepoMock.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(caregiver);
        _patientRepoMock.Setup(r => r.CountByCaregiverIdAsync("1")).ReturnsAsync(0);

        var result = await _service.ChangePlanAsync("1", new ChangePlanRequest { Plan = "Premium" });

        result.Plan.Should().Be("Premium");
        caregiver.Plan.Should().Be(Plan.Premium);
        _caregiverRepoMock.Verify(r => r.UpdateAsync(caregiver), Times.Once);
    }

    [Fact]
    public async Task ChangePlanAsync_InvalidPlan_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.ChangePlanAsync("1", new ChangePlanRequest { Plan = "Ultimate" }));
    }

    [Fact]
    public async Task ChangePlanAsync_DowngradeExceedsPatientLimit_ThrowsValidationException()
    {
        var caregiver = new Caregiver
        {
            Id = "1",
            Email = "test@test.com",
            Plan = Plan.Premium
        };

        _caregiverRepoMock.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(caregiver);
        _patientRepoMock.Setup(r => r.CountByCaregiverIdAsync("1")).ReturnsAsync(5);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.ChangePlanAsync("1", new ChangePlanRequest { Plan = "Standard" }));
    }
}
