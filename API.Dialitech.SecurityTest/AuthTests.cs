using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using API.Dialitech.Application.DTOs;
using FluentAssertions;

namespace API.Dialitech.SecurityTest;

public class AuthTests : IClassFixture<SecurityWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthTests(SecurityWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSecuredEndpoint_NoToken_ShouldReturnUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSecuredEndpoint_InvalidToken_ShouldReturnUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "invalid-token-value");

        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSecuredEndpoint_ValidToken_ShouldReturnOk()
    {
        var token = await GetValidTokenAsync();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthDataEndpoint_NoToken_ShouldReturnUnauthorized()
    {
        var response = await _client.GetAsync("/api/health-data/someuser");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AlertsEndpoint_NoToken_ShouldReturnUnauthorized()
    {
        var response = await _client.GetAsync("/api/alerts/someuser");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AuthMe_WithValidToken_ShouldReturnUserInfo()
    {
        var token = await GetValidTokenAsync();

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        content.Should().NotBeNull();
        content!.ContainsKey("userId").Should().BeTrue();
    }

    [Fact]
    public async Task AuthMe_NoToken_ShouldReturnUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<string> GetValidTokenAsync()
    {
        var email = $"sec.{Guid.NewGuid()}@test.com";
        var registerPayload = new { name = "Security Tester", email, password = "Test123!", age = 30 };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerPayload);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        return authResponse!.Token;
    }
}
