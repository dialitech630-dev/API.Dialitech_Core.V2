using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using API.Dialitech.Application.DTOs;
using FluentAssertions;

namespace API.Dialitech.IntegrationTest.Controllers;

public class DevicesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DevicesControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<(string token, string patientId)> CreatePatientAsync()
    {
        var email = $"dev.{Guid.NewGuid()}@test.com";
        var regPayload = new { name = "Device Test", email, password = "Test123!", plan = "Premium" };
        var regResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", regPayload);
        var authResponse = await regResponse.Content.ReadFromJsonAsync<AuthResponse>();
        var token = authResponse!.Token;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var patientPayload = new { name = "Device Patient", age = 30, gender = "Male", notes = "" };
        var patientResponse = await _client.PostAsJsonAsync("/api/v1/patients", patientPayload);
        var patient = await patientResponse.Content.ReadFromJsonAsync<PatientDto>();

        return (token, patient!.Id);
    }

    [Fact]
    public async Task GenerateCode_ShouldReturnCode()
    {
        var (token, patientId) = await CreatePatientAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync($"/api/v1/patients/{patientId}/generate-code", new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var codeResponse = await response.Content.ReadFromJsonAsync<GenerateCodeResponse>();
        codeResponse.Should().NotBeNull();
        codeResponse!.Code.Should().HaveLength(6);
        codeResponse.ExpiresInSeconds.Should().Be(300);
    }

    [Fact]
    public async Task ValidateCode_ShouldReturnValid()
    {
        var (token, patientId) = await CreatePatientAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var codeResponse = await _client.PostAsJsonAsync($"/api/v1/patients/{patientId}/generate-code", new { });
        var code = (await codeResponse.Content.ReadFromJsonAsync<GenerateCodeResponse>())!.Code;

        _client.DefaultRequestHeaders.Authorization = null;

        var validatePayload = new { code };
        var response = await _client.PostAsJsonAsync("/api/v1/patients/validate-code", validatePayload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var validateResponse = await response.Content.ReadFromJsonAsync<ValidateCodeResponse>();
        validateResponse!.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task FullFlow_GenerateValidateLink_Success()
    {
        var (token, patientId) = await CreatePatientAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var codeResponse = await _client.PostAsJsonAsync($"/api/v1/patients/{patientId}/generate-code", new { });
        var code = (await codeResponse.Content.ReadFromJsonAsync<GenerateCodeResponse>())!.Code;

        _client.DefaultRequestHeaders.Authorization = null;

        var linkPayload = new { code, serialNumber = $"SN-{Guid.NewGuid():N}" };
        var linkResponse = await _client.PostAsJsonAsync("/api/v1/devices/link", linkPayload);

        linkResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var linkResult = await linkResponse.Content.ReadFromJsonAsync<LinkDeviceResponse>();
        linkResult!.Linked.Should().BeTrue();
    }
}
