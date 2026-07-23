using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using API.Dialitech.Application.DTOs;
using FluentAssertions;

namespace API.Dialitech.IntegrationTest.Controllers;

public class AlertsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AlertsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<(string userId, string token)> RegisterAndCreateAlertAsync()
    {
        var email = $"alert.{Guid.NewGuid()}@test.com";
        var registerPayload = new { name = "Alert Tester", email, password = "Test123!", age = 45 };
        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", registerPayload);
        var authResponse = await regResponse.Content.ReadFromJsonAsync<AuthResponse>();
        var userId = authResponse!.User.Id;
        var token = authResponse.Token;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var hdPayload = new { userId, heartRate = 140, spO2 = 85.0, activityLevel = 90, timestamp = DateTime.UtcNow.AddMinutes(-5) };
        await _client.PostAsJsonAsync("/api/health-data", hdPayload);

        return (userId, token);
    }

    [Fact]
    public async Task GetByUser_ShouldReturnAlerts()
    {
        var (userId, token) = await RegisterAndCreateAlertAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/alerts/{userId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var alerts = await response.Content.ReadFromJsonAsync<List<AlertDto>>();
        alerts.Should().NotBeNull();
        alerts.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetByUser_NoExtremeValues_NoAlerts()
    {
        var email = $"noalert.{Guid.NewGuid()}@test.com";
        var registerPayload = new { name = "No Alert", email, password = "Test123!", age = 30 };
        var regResponse = await _client.PostAsJsonAsync("/api/auth/register", registerPayload);
        var authResponse = await regResponse.Content.ReadFromJsonAsync<AuthResponse>();
        var userId = authResponse!.User.Id;
        var token = authResponse.Token;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var hdPayload = new { userId, heartRate = 72, spO2 = 98.0, activityLevel = 40, timestamp = DateTime.UtcNow.AddMinutes(-5) };
        await _client.PostAsJsonAsync("/api/health-data", hdPayload);

        var response = await _client.GetAsync($"/api/alerts/{userId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var alerts = await response.Content.ReadFromJsonAsync<List<AlertDto>>();
        alerts.Should().BeEmpty();
    }

    [Fact]
    public async Task NoAuth_ShouldReturnUnauthorized()
    {
        var response = await _client.GetAsync("/api/alerts/someuser");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
