using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using API.Dialitech.Application.DTOs;
using FluentAssertions;

namespace API.Dialitech.SecurityTest;

public class DashboardSecurityTests : IClassFixture<SecurityWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DashboardSecurityTests(SecurityWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ReadingsEndpoint_NoToken_ShouldReturnUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/dashboard/some-patient/readings");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReadingsEndpoint_AnotherCaregiver_ShouldReturnNotFound()
    {
        var email1 = $"dr1.{Guid.NewGuid()}@test.com";
        var reg1 = new { name = "DR1", email = email1, password = "Test123!", plan = "Premium" };
        var auth1 = await (await _client.PostAsJsonAsync("/api/v1/auth/register", reg1))
            .Content.ReadFromJsonAsync<AuthResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth1!.Token);
        var patient = await (await _client.PostAsJsonAsync("/api/v1/patients",
            new { name = "P1", age = 30, gender = "Male", notes = "" }))
            .Content.ReadFromJsonAsync<PatientDto>();

        var email2 = $"dr2.{Guid.NewGuid()}@test.com";
        var reg2 = new { name = "DR2", email = email2, password = "Test123!", plan = "Premium" };
        var auth2 = await (await _client.PostAsJsonAsync("/api/v1/auth/register", reg2))
            .Content.ReadFromJsonAsync<AuthResponse>();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth2!.Token);
        var response = await _client.GetAsync($"/api/v1/dashboard/{patient!.Id}/readings");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
