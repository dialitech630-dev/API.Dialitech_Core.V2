using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using API.Dialitech.Application.DTOs;
using FluentAssertions;

namespace API.Dialitech.IntegrationTest.Controllers;

public class EndToEndFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public EndToEndFlowTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> GetTokenAsync()
    {
        var email = $"e2e.{Guid.NewGuid()}@test.com";
        var payload = new { name = "E2E Test", email, password = "Test123!", plan = "Premium" };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", payload);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return authResponse!.Token;
    }

    [Fact]
    public async Task FullFlow_RegisterCreatePatientGenerateCodeLinkDevice()
    {
        var token = await GetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createPayload = new { name = "E2E Patient", age = 30, gender = "Male", notes = "" };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/patients", createPayload);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var patient = await createResponse.Content.ReadFromJsonAsync<PatientDto>();

        var codeResponse = await _client.PostAsJsonAsync($"/api/v1/patients/{patient!.Id}/generate-code", new { });
        codeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var codeResult = await codeResponse.Content.ReadFromJsonAsync<GenerateCodeResponse>();

        _client.DefaultRequestHeaders.Authorization = null;

        var validatePayload = new { code = codeResult!.Code };
        var validateResponse = await _client.PostAsJsonAsync("/api/v1/patients/validate-code", validatePayload);
        validateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var validateResult = await validateResponse.Content.ReadFromJsonAsync<ValidateCodeResponse>();
        validateResult!.IsValid.Should().BeTrue();

        var serialNumber = $"SN-{Guid.NewGuid():N}";
        var linkPayload = new { code = codeResult.Code, serialNumber };
        var linkResponse = await _client.PostAsJsonAsync("/api/v1/devices/link", linkPayload);
        linkResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var linkResult = await linkResponse.Content.ReadFromJsonAsync<LinkDeviceResponse>();
        linkResult!.Linked.Should().BeTrue();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var getResponse = await _client.GetAsync($"/api/v1/patients/{patient.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var getPatient = await getResponse.Content.ReadFromJsonAsync<PatientDto>();
        getPatient!.DeviceSerialNumber.Should().Be(serialNumber);
    }

    [Fact]
    public async Task FullFlow_RegisterCreatePatientSendHealthDataCheckAlerts()
    {
        var token = await GetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createPayload = new { name = "Alert Patient", age = 30, gender = "Male", notes = "" };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/patients", createPayload);
        var patient = await createResponse.Content.ReadFromJsonAsync<PatientDto>();

        var codeResponse = await _client.PostAsJsonAsync($"/api/v1/patients/{patient!.Id}/generate-code", new { });
        var codeResult = await codeResponse.Content.ReadFromJsonAsync<GenerateCodeResponse>();

        _client.DefaultRequestHeaders.Authorization = null;

        var batchPayload = new
        {
            patientCode = codeResult!.Code,
            data = new[]
            {
                new { heartRate = 130.0, oxygen = 97.0, activity = 80.0, timestamp = DateTime.UtcNow }
            }
        };
        var batchResponse = await _client.PostAsJsonAsync("/api/v1/health-data/batch", batchPayload);
        batchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var batchResult = await batchResponse.Content.ReadFromJsonAsync<BatchResponse>();
        batchResult!.AlertsTriggered.Should().Be(1);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var alertsResponse = await _client.GetAsync($"/api/v1/alerts/{patient.Id}");
        alertsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var alerts = await alertsResponse.Content.ReadFromJsonAsync<List<AlertDto>>();
        alerts.Should().NotBeEmpty();
    }

    [Fact]
    public async Task FullFlow_RegisterCreateDeletePatient()
    {
        var token = await GetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createPayload = new { name = "To Delete", age = 25, gender = "Female", notes = "" };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/patients", createPayload);
        var patient = await createResponse.Content.ReadFromJsonAsync<PatientDto>();

        var deleteResponse = await _client.DeleteAsync($"/api/v1/patients/{patient!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/v1/patients/{patient.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task FullFlow_DashboardSummaryAfterCreatingPatients()
    {
        var token = await GetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createPayload1 = new { name = "Dash Patient 1", age = 30, gender = "Male", notes = "" };
        await _client.PostAsJsonAsync("/api/v1/patients", createPayload1);

        var createPayload2 = new { name = "Dash Patient 2", age = 40, gender = "Female", notes = "" };
        await _client.PostAsJsonAsync("/api/v1/patients", createPayload2);

        var dashResponse = await _client.GetAsync("/api/v1/dashboard");
        dashResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = await dashResponse.Content.ReadFromJsonAsync<DashboardSummary>();
        summary!.TotalPatients.Should().BeGreaterThanOrEqualTo(2);
    }
}
