using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using API.Dialitech.Application.DTOs;
using FluentAssertions;

namespace API.Dialitech.IntegrationTest.Controllers;

public class HealthDataControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthDataControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<(string userId, string token)> RegisterAndLoginAsync()
    {
        var email = $"hd.{Guid.NewGuid()}@test.com";
        var registerPayload = new { name = "Health Tester", email, password = "Test123!", age = 40 };
        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", registerPayload);
        regResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var authResponse = await regResponse.Content.ReadFromJsonAsync<AuthResponse>();
        return (authResponse!.User.Id, authResponse.Token);
    }

    [Fact]
    public async Task Create_ShouldReturnCreated()
    {
        var (userId, token) = await RegisterAndLoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new
        {
            userId,
            heartRate = 75,
            spO2 = 98.0,
            activityLevel = 50,
            timestamp = DateTime.UtcNow.AddMinutes(-10)
        };
        var response = await _client.PostAsJsonAsync("/api/health-data", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var record = await response.Content.ReadFromJsonAsync<HealthDataDto>();
        record.Should().NotBeNull();
        record!.HeartRate.Should().Be(75);
    }

    [Fact]
    public async Task GetByUser_ShouldReturnRecords()
    {
        var (userId, token) = await RegisterAndLoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new { userId, heartRate = 80, spO2 = 97.0, activityLevel = 60, timestamp = DateTime.UtcNow.AddMinutes(-5) };
        await _client.PostAsJsonAsync("/api/health-data", payload);

        var response = await _client.GetAsync($"/api/health-data/{userId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var records = await response.Content.ReadFromJsonAsync<List<HealthDataDto>>();
        records.Should().NotBeNull();
        records.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetLatest_ShouldReturnLatestRecord()
    {
        var (userId, token) = await RegisterAndLoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload1 = new { userId, heartRate = 70, spO2 = 98.0, activityLevel = 30, timestamp = DateTime.UtcNow.AddMinutes(-10) };
        var payload2 = new { userId, heartRate = 85, spO2 = 96.0, activityLevel = 70, timestamp = DateTime.UtcNow.AddMinutes(-1) };
        await _client.PostAsJsonAsync("/api/health-data", payload1);
        await _client.PostAsJsonAsync("/api/health-data", payload2);

        var response = await _client.GetAsync($"/api/health-data/{userId}/latest");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var record = await response.Content.ReadFromJsonAsync<HealthDataDto>();
        record.Should().NotBeNull();
        record!.HeartRate.Should().Be(85);
    }

    [Fact]
    public async Task GetByDateRange_ShouldReturnFilteredRecords()
    {
        var (userId, token) = await RegisterAndLoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new { userId, heartRate = 72, spO2 = 97.5, activityLevel = 40, timestamp = DateTime.UtcNow.AddDays(-2) };
        await _client.PostAsJsonAsync("/api/health-data", payload);

        var start = DateTime.UtcNow.AddDays(-3);
        var end = DateTime.UtcNow;
        var response = await _client.GetAsync($"/api/health-data/{userId}/range?start={start:O}&end={end:O}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var records = await response.Content.ReadFromJsonAsync<List<HealthDataDto>>();
        records.Should().NotBeNull();
    }

    [Fact]
    public async Task NoAuth_ShouldReturnUnauthorized()
    {
        var response = await _client.GetAsync("/api/health-data/someuser");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
