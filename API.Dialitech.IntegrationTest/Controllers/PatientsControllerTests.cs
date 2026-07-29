using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using API.Dialitech.Application.DTOs;
using FluentAssertions;

namespace API.Dialitech.IntegrationTest.Controllers;

public class PatientsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PatientsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> GetTokenAsync()
    {
        var email = $"pat.{Guid.NewGuid()}@test.com";
        var payload = new { name = "Patient Test", email, password = "Test123!", plan = "Premium" };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", payload);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return authResponse!.Token;
    }

    [Fact]
    public async Task CreatePatient_ShouldReturnCreated()
    {
        var token = await GetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new { name = "John", age = 30, gender = "Male", notes = "" };
        var response = await _client.PostAsJsonAsync("/api/v1/patients", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var patient = await response.Content.ReadFromJsonAsync<PatientDto>();
        patient.Should().NotBeNull();
        patient!.Name.Should().Be("John");
    }

    [Fact]
    public async Task GetPatients_ShouldReturnList()
    {
        var token = await GetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/v1/patients");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var patients = await response.Content.ReadFromJsonAsync<List<PatientDto>>();
        patients.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPatientById_Existing_ShouldReturnOk()
    {
        var token = await GetTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createPayload = new { name = "Jane", age = 25, gender = "Female", notes = "" };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/patients", createPayload);
        var patient = await createResponse.Content.ReadFromJsonAsync<PatientDto>();

        var response = await _client.GetAsync($"/api/v1/patients/{patient!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task NoAuth_ShouldReturnUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/patients");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
