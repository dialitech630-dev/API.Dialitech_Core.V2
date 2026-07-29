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

    private async Task<(string token, string patientId)> CreateSetupAsync()
    {
        var email = $"alert.{Guid.NewGuid()}@test.com";
        var regPayload = new { name = "Alert Test", email, password = "Test123!", plan = "Premium" };
        var regResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", regPayload);
        var authResponse = await regResponse.Content.ReadFromJsonAsync<AuthResponse>();
        var token = authResponse!.Token;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var patientPayload = new { name = "Alert Patient", age = 30, gender = "Male", notes = "" };
        var patientResponse = await _client.PostAsJsonAsync("/api/v1/patients", patientPayload);
        var patient = await patientResponse.Content.ReadFromJsonAsync<PatientDto>();

        var codeResponse = await _client.PostAsJsonAsync($"/api/v1/patients/{patient!.Id}/generate-code", new { });
        var code = (await codeResponse.Content.ReadFromJsonAsync<GenerateCodeResponse>())!.Code;

        _client.DefaultRequestHeaders.Authorization = null;

        var batchPayload = new
        {
            patientCode = code,
            data = new[] { new { heartRate = 130.0, oxygen = 97.0, activity = 80.0, timestamp = DateTime.UtcNow } }
        };
        await _client.PostAsJsonAsync("/api/v1/health-data/batch", batchPayload);

        return (token, patient.Id);
    }

    [Fact]
    public async Task GetAlerts_ShouldReturnList()
    {
        var (token, _) = await CreateSetupAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/alerts");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var alerts = await response.Content.ReadFromJsonAsync<List<AlertDto>>();
        alerts.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAlerts_NoAuth_ShouldReturnUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/alerts");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAlertsByPatient_ShouldReturnAlerts()
    {
        var (token, patientId) = await CreateSetupAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/v1/alerts/{patientId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var alerts = await response.Content.ReadFromJsonAsync<List<AlertDto>>();
        alerts.Should().NotBeNull();
    }
}
