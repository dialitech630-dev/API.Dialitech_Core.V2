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
}
