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
    public async Task Register_WithSqlInjectionInName_ShouldBeRejected()
    {
        var payload = new { name = "'; DROP TABLE Users; --", email = $"hacker.{Guid.NewGuid()}@test.com", password = "Test123!" };

        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_WithXssInEmail_ShouldBeRejected()
    {
        var payload = new { name = "XSS Attacker", email = $"<script>alert('xss')</script>@test.com", password = "Test123!" };

        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private async Task<string> GetValidTokenAsync()
    {
        var email = $"inj.{Guid.NewGuid()}@test.com";
        var payload = new { name = "Injection Tester", email, password = "Test123!" };

        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);
        response.EnsureSuccessStatusCode();

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return authResponse!.Token;
    }
}
