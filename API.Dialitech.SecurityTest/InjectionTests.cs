using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using API.Dialitech.Application.DTOs;
using FluentAssertions;

namespace API.Dialitech.SecurityTest;

public class InjectionTests : IClassFixture<SecurityWebApplicationFactory>
{
    private readonly HttpClient _client;

    public InjectionTests(SecurityWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUserById_NoSqlInjectionDollarOperator_ShouldReturnBadRequest()
    {
        var token = await GetValidTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/users/$ne=null");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetUserById_NoSqlInjectionWithRegex_ShouldReturnBadRequest()
    {
        var token = await GetValidTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/users/$regex=.*");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithSqlInjectionInName_ShouldBeAccepted()
    {
        var payload = new { name = "'; DROP TABLE Users; --", email = $"hacker.{Guid.NewGuid()}@test.com", password = "Test123!", age = 30 };

        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_WithXssInEmail_ShouldBeAccepted()
    {
        var payload = new { name = "XSS Attacker", email = $"<script>alert('xss')</script>@test.com", password = "Test123!", age = 30 };

        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task HealthData_WithInvalidId_ShouldReturnBadRequest()
    {
        var token = await GetValidTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/health-data/$ne=null");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task HealthData_CreateWithExtremeValues_ShouldBeValidated()
    {
        var token = await GetValidTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var payload = new { userId = "test", heartRate = 999, spO2 = 999.0, activityLevel = 999, timestamp = DateTime.UtcNow.AddMinutes(-5) };
        var response = await _client.PostAsJsonAsync("/api/health-data", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task HealthData_CreateWithFutureTimestamp_ShouldBeRejected()
    {
        var token = await GetValidTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var payload = new { userId = "test", heartRate = 80, spO2 = 97.0, activityLevel = 50, timestamp = DateTime.UtcNow.AddDays(1) };
        var response = await _client.PostAsJsonAsync("/api/health-data", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Alerts_WithDollarInUserId_ShouldReturnBadRequest()
    {
        var token = await GetValidTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/alerts/$gt=");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<string> GetValidTokenAsync()
    {
        var email = $"inj.{Guid.NewGuid()}@test.com";
        var payload = new { name = "Injection Tester", email, password = "Test123!", age = 30 };

        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);
        response.EnsureSuccessStatusCode();

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return authResponse!.Token;
    }
}
