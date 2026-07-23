using System.Net;
using System.Net.Http.Json;
using API.Dialitech.Application.DTOs;
using FluentAssertions;

namespace API.Dialitech.IntegrationTest.Controllers;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ShouldReturnCreatedWithToken()
    {
        var payload = new { name = "Test User", email = $"reg.{Guid.NewGuid()}@test.com", password = "Test123!", age = 30 };

        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();
        authResponse!.Token.Should().NotBeNullOrEmpty();
        authResponse.User.Name.Should().Be("Test User");
        authResponse.User.Age.Should().Be(30);
        authResponse.User.Id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_InvalidAge_ShouldReturnBadRequest()
    {
        var payload = new { name = "Bad Age", email = $"age.{Guid.NewGuid()}@test.com", password = "Test123!", age = 0 };

        var response = await _client.PostAsJsonAsync("/api/auth/register", payload);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ValidCredentials_ShouldReturnToken()
    {
        var email = $"login.{Guid.NewGuid()}@test.com";
        var registerPayload = new { name = "Login Test", email, password = "Test123!", age = 28 };
        await _client.PostAsJsonAsync("/api/auth/register", registerPayload);

        var loginPayload = new { email, password = "Test123!" };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginPayload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();
        authResponse!.Token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_InvalidPassword_ShouldReturnUnauthorized()
    {
        var email = $"badpwd.{Guid.NewGuid()}@test.com";
        var registerPayload = new { name = "Bad Pwd", email, password = "Test123!", age = 30 };
        await _client.PostAsJsonAsync("/api/auth/register", registerPayload);

        var loginPayload = new { email, password = "wrongpassword" };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginPayload);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithValidToken_ShouldReturnUserInfo()
    {
        var email = $"me.{Guid.NewGuid()}@test.com";
        var registerPayload = new { name = "Me Test", email, password = "Test123!", age = 35 };
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerPayload);
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.Token);

        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Me_NoToken_ShouldReturnUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
