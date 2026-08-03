using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using API.Dialitech.Application.DTOs;
using FluentAssertions;

namespace API.Dialitech.IntegrationTest.Controllers;

public class DashboardControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DashboardControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> GetTokenAsync()
    {
        var email = $"dash.{Guid.NewGuid()}@test.com";
        var payload = new { name = "Dashboard Test", email, password = "Test123!", plan = "Premium" };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", payload);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return authResponse!.Token;
    }

    [Fact]
    public async Task GetDashboard_ShouldReturnSummary()
    {
        var token = await GetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = await response.Content.ReadFromJsonAsync<DashboardSummary>();
        summary.Should().NotBeNull();
        summary!.TotalPatients.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetDashboard_NoAuth_ShouldReturnUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/dashboard");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPatientReadings_NoReadings_ShouldReturnEmpty()
    {
        var token = await GetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync("/api/v1/patients",
            new { name = "Readings Patient", age = 40, gender = "Femenino", notes = "" });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var patient = await createResponse.Content.ReadFromJsonAsync<PatientDto>();

        var response = await _client.GetAsync($"/api/v1/dashboard/{patient!.Id}/readings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var readings = await response.Content.ReadFromJsonAsync<ReadingsResponse>();
        readings.Should().NotBeNull();
        readings!.Readings.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPatientReadings_OtherCaregiver_ShouldReturnNotFound()
    {
        var token1 = await GetTokenAsync();
        var token2 = await GetTokenAsync();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token1);
        var createResponse = await _client.PostAsJsonAsync("/api/v1/patients",
            new { name = "Owner Patient", age = 40, gender = "Masculino", notes = "" });
        var patient = await createResponse.Content.ReadFromJsonAsync<PatientDto>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token2);
        var response = await _client.GetAsync($"/api/v1/dashboard/{patient!.Id}/readings");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPatientReadings_NoAuth_ShouldReturnUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/dashboard/whatever/readings");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
