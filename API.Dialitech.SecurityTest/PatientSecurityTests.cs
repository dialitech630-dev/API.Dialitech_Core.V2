using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using API.Dialitech.Application.DTOs;
using FluentAssertions;

namespace API.Dialitech.SecurityTest;

public class PatientSecurityTests : IClassFixture<SecurityWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PatientSecurityTests(SecurityWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AccessPatientFromAnotherCaregiver_ShouldReturnNotFound()
    {
        var email1 = $"cg1.{Guid.NewGuid()}@test.com";
        var reg1 = new { name = "CG1", email = email1, password = "Test123!", plan = "Premium" };
        var reg1Response = await _client.PostAsJsonAsync("/api/v1/auth/register", reg1);
        var auth1 = await reg1Response.Content.ReadFromJsonAsync<AuthResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth1!.Token);
        var createPayload = new { name = "Patient1", age = 30, gender = "Male", notes = "" };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/patients", createPayload);
        var patient = await createResponse.Content.ReadFromJsonAsync<PatientDto>();

        var email2 = $"cg2.{Guid.NewGuid()}@test.com";
        var reg2 = new { name = "CG2", email = email2, password = "Test123!", plan = "Premium" };
        var reg2Response = await _client.PostAsJsonAsync("/api/v1/auth/register", reg2);
        var auth2 = await reg2Response.Content.ReadFromJsonAsync<AuthResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth2!.Token);
        var response = await _client.GetAsync($"/api/v1/patients/{patient!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePatient_FromAnotherCaregiver_ShouldReturnNotFound()
    {
        var email1 = $"cg1.{Guid.NewGuid()}@test.com";
        var reg1 = new { name = "CG1", email = email1, password = "Test123!", plan = "Premium" };
        var reg1Response = await _client.PostAsJsonAsync("/api/v1/auth/register", reg1);
        var auth1 = await reg1Response.Content.ReadFromJsonAsync<AuthResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth1!.Token);
        var createPayload = new { name = "Patient1", age = 30, gender = "Male", notes = "" };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/patients", createPayload);
        var patient = await createResponse.Content.ReadFromJsonAsync<PatientDto>();

        var email2 = $"cg2.{Guid.NewGuid()}@test.com";
        var reg2 = new { name = "CG2", email = email2, password = "Test123!", plan = "Premium" };
        var reg2Response = await _client.PostAsJsonAsync("/api/v1/auth/register", reg2);
        var auth2 = await reg2Response.Content.ReadFromJsonAsync<AuthResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth2!.Token);
        var response = await _client.DeleteAsync($"/api/v1/patients/{patient!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GenerateCode_FromAnotherCaregiver_ShouldReturnNotFound()
    {
        var email1 = $"cg1.{Guid.NewGuid()}@test.com";
        var reg1 = new { name = "CG1", email = email1, password = "Test123!", plan = "Premium" };
        var reg1Response = await _client.PostAsJsonAsync("/api/v1/auth/register", reg1);
        var auth1 = await reg1Response.Content.ReadFromJsonAsync<AuthResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth1!.Token);
        var createPayload = new { name = "Patient1", age = 30, gender = "Male", notes = "" };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/patients", createPayload);
        var patient = await createResponse.Content.ReadFromJsonAsync<PatientDto>();

        var email2 = $"cg2.{Guid.NewGuid()}@test.com";
        var reg2 = new { name = "CG2", email = email2, password = "Test123!", plan = "Premium" };
        var reg2Response = await _client.PostAsJsonAsync("/api/v1/auth/register", reg2);
        var auth2 = await reg2Response.Content.ReadFromJsonAsync<AuthResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth2!.Token);
        var response = await _client.PostAsJsonAsync($"/api/v1/patients/{patient!.Id}/generate-code", new { });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DashboardPatientStatus_FromAnotherCaregiver_ShouldReturnNotFound()
    {
        var email1 = $"cg1.{Guid.NewGuid()}@test.com";
        var reg1 = new { name = "CG1", email = email1, password = "Test123!", plan = "Premium" };
        var reg1Response = await _client.PostAsJsonAsync("/api/v1/auth/register", reg1);
        var auth1 = await reg1Response.Content.ReadFromJsonAsync<AuthResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth1!.Token);
        var createPayload = new { name = "Patient1", age = 30, gender = "Male", notes = "" };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/patients", createPayload);
        var patient = await createResponse.Content.ReadFromJsonAsync<PatientDto>();

        var email2 = $"cg2.{Guid.NewGuid()}@test.com";
        var reg2 = new { name = "CG2", email = email2, password = "Test123!", plan = "Premium" };
        var reg2Response = await _client.PostAsJsonAsync("/api/v1/auth/register", reg2);
        var auth2 = await reg2Response.Content.ReadFromJsonAsync<AuthResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth2!.Token);
        var response = await _client.GetAsync($"/api/v1/dashboard/{patient!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task InvalidId_WithDollarSign_ShouldReturnBadRequest()
    {
        var email = $"cg.{Guid.NewGuid()}@test.com";
        var reg = new { name = "CG", email, password = "Test123!", plan = "Premium" };
        var regResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", reg);
        var auth = await regResponse.Content.ReadFromJsonAsync<AuthResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        var response = await _client.GetAsync("/api/v1/patients/$invalid");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
