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

    private async Task<string> GetPatientCodeAsync()
    {
        var email = $"hd.{Guid.NewGuid()}@test.com";
        var regPayload = new { name = "HD Test", email, password = "Test123!", plan = "Premium" };
        var regResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", regPayload);
        var authResponse = await regResponse.Content.ReadFromJsonAsync<AuthResponse>();
        var token = authResponse!.Token;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var patientPayload = new { name = "HD Patient", age = 30, gender = "Male", notes = "" };
        var patientResponse = await _client.PostAsJsonAsync("/api/v1/patients", patientPayload);
        var patient = await patientResponse.Content.ReadFromJsonAsync<PatientDto>();

        var codeResponse = await _client.PostAsJsonAsync($"/api/v1/patients/{patient!.Id}/generate-code", new { });
        var code = (await codeResponse.Content.ReadFromJsonAsync<GenerateCodeResponse>())!.Code;

        _client.DefaultRequestHeaders.Authorization = null;
        return code;
    }

    [Fact]
    public async Task PostBatch_ValidData_ReturnsProcessed()
    {
        var code = await GetPatientCodeAsync();

        var request = new
        {
            patientCode = code,
            data = new[]
            {
                new { heartRate = 75.0, oxygen = 98.0, activity = 50.0, timestamp = DateTime.UtcNow }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/v1/health-data/batch", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BatchResponse>();
        result.Should().NotBeNull();
        result!.Status.Should().Be("processed");
    }

    [Fact]
    public async Task PostBatch_InvalidCode_ReturnsNotFound()
    {
        var request = new
        {
            patientCode = "INVALID",
            data = new[]
            {
                new { heartRate = 75.0, oxygen = 98.0, activity = 50.0, timestamp = DateTime.UtcNow }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/v1/health-data/batch", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostBatch_TriggersAlert_OnHighHR()
    {
        var code = await GetPatientCodeAsync();

        var request = new
        {
            patientCode = code,
            data = new[]
            {
                new { heartRate = 130.0, oxygen = 97.0, activity = 80.0, timestamp = DateTime.UtcNow }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/v1/health-data/batch", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<BatchResponse>();
        result!.AlertsTriggered.Should().Be(1);
    }
}
