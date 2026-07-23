using API.Dialitech.Application.Commands.HealthData.CreateHealthData;
using API.Dialitech.Application.DTOs;
using API.Dialitech.Application.Interfaces;
using API.Dialitech.Application.Queries.HealthData.GetByDateRange;
using API.Dialitech.Application.Queries.HealthData.GetByUser;
using API.Dialitech.Application.Queries.HealthData.GetLatest;
using API.Dialitech.Application.Services;
using FluentAssertions;
using MediatR;
using Moq;

namespace API.Dialitech.UnitTest.Services;

public class HealthDataServiceTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly IHealthDataService _service;

    public HealthDataServiceTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _service = new HealthDataService(_mediatorMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldSendCreateHealthDataCommand()
    {
        var dto = new CreateHealthDataDto
        {
            UserId = "user1",
            HeartRate = 80,
            SpO2 = 97.5,
            ActivityLevel = 50,
            Timestamp = DateTime.UtcNow.AddMinutes(-5)
        };
        var expected = new HealthDataDto { Id = "abc123", UserId = "user1", HeartRate = 80, SpO2 = 97.5, ActivityLevel = 50 };

        _mediatorMock.Setup(m => m.Send(It.IsAny<CreateHealthDataCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _service.CreateAsync(dto);

        result.Should().NotBeNull();
        result.HeartRate.Should().Be(80);
        _mediatorMock.Verify(m => m.Send(
            It.Is<CreateHealthDataCommand>(c => c.UserId == "user1" && c.HeartRate == 80),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnRecords()
    {
        var records = new List<HealthDataDto>
        {
            new() { Id = "1", UserId = "user1", HeartRate = 75, SpO2 = 98, ActivityLevel = 40 },
            new() { Id = "2", UserId = "user1", HeartRate = 80, SpO2 = 97, ActivityLevel = 55 }
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetHealthDataByUserQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(records);

        var result = await _service.GetByUserIdAsync("user1");

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByUserIdAsync_NoRecords_ShouldReturnEmpty()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetHealthDataByUserQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _service.GetByUserIdAsync("nonexistent");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLatestAsync_ExistingUser_ShouldReturnLatestRecord()
    {
        var record = new HealthDataDto { Id = "1", UserId = "user1", HeartRate = 72, SpO2 = 98, ActivityLevel = 30 };

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetLatestHealthDataQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(record);

        var result = await _service.GetLatestAsync("user1");

        result.Should().NotBeNull();
        result!.HeartRate.Should().Be(72);
    }

    [Fact]
    public async Task GetLatestAsync_NoRecords_ShouldReturnNull()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetLatestHealthDataQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((HealthDataDto?)null);

        var result = await _service.GetLatestAsync("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByDateRangeAsync_ShouldSendQueryWithDates()
    {
        var start = DateTime.UtcNow.AddDays(-7);
        var end = DateTime.UtcNow;
        var records = new List<HealthDataDto>
        {
            new() { Id = "1", UserId = "user1", HeartRate = 78, SpO2 = 96, ActivityLevel = 45 }
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetHealthDataByDateRangeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(records);

        var result = await _service.GetByDateRangeAsync("user1", start, end);

        result.Should().HaveCount(1);
        _mediatorMock.Verify(m => m.Send(
            It.Is<GetHealthDataByDateRangeQuery>(q => q.UserId == "user1" && q.Start == start && q.End == end),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByDateRangeAsync_NoDates_ShouldSendNullStartAndEnd()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetHealthDataByDateRangeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _service.GetByDateRangeAsync("user1", null, null);

        result.Should().BeEmpty();
        _mediatorMock.Verify(m => m.Send(
            It.Is<GetHealthDataByDateRangeQuery>(q => q.Start == null && q.End == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
