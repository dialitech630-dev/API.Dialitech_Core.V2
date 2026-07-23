using API.Dialitech.Application.DTOs;
using API.Dialitech.Application.Interfaces;
using API.Dialitech.Application.Queries.Alerts.GetAlertsByUser;
using API.Dialitech.Application.Services;
using FluentAssertions;
using MediatR;
using Moq;

namespace API.Dialitech.UnitTest.Services;

public class AlertServiceTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly IAlertService _service;

    public AlertServiceTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _service = new AlertService(_mediatorMock.Object);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnAlerts()
    {
        var alerts = new List<AlertDto>
        {
            new() { Id = "1", UserId = "user1", Type = "warning", Message = "HR high", Severity = 2 },
            new() { Id = "2", UserId = "user1", Type = "critical", Message = "SpO2 low", Severity = 3 }
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAlertsByUserQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(alerts);

        var result = await _service.GetByUserIdAsync("user1");

        result.Should().HaveCount(2);
        result.First().Type.Should().Be("warning");
        _mediatorMock.Verify(m => m.Send(
            It.Is<GetAlertsByUserQuery>(q => q.UserId == "user1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByUserIdAsync_NoAlerts_ShouldReturnEmpty()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAlertsByUserQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _service.GetByUserIdAsync("user1");

        result.Should().BeEmpty();
    }
}
