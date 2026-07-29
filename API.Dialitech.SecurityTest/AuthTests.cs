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
        var response = await _client.GetAsync("/api/v1/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSecuredEndpoint_InvalidToken_ShouldReturnUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "invalid-token-value");

        var response = await _client.GetAsync("/api/v1/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_WithSqlInjection_ShouldBeAccepted()
    {
        var payload = new { name = "'; DROP TABLE Caregivers; --", email = $"hack.{Guid.NewGuid()}@test.com", password = "Test123!", plan = "Standard" };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Login_WrongPassword_ShouldReturnUnauthorized()
    {
        var email = $"sec.{Guid.NewGuid()}@test.com";
        var registerPayload = new { name = "Security Test", email, password = "Test123!", plan = "Standard" };
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerPayload);

        var loginPayload = new { email, password = "wrongpassword" };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginPayload);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PatientEndpoint_NoToken_ShouldReturnUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/patients");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<string> GetValidTokenAsync()
    {
        var email = $"tok.{Guid.NewGuid()}@test.com";
        var registerPayload = new { name = "Token Test", email, password = "Test123!", plan = "Standard" };
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", registerPayload);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        return authResponse!.Token;
    }
}
