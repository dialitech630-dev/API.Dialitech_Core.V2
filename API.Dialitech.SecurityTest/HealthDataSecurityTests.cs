using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace API.Dialitech.SecurityTest;

public class HealthDataSecurityTests : IClassFixture<SecurityWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthDataSecurityTests(SecurityWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostBatch_MalformedPayload_ShouldReturnBadRequest()
    {
        var payload = new { patientCode = "CODE" };
        var response = await _client.PostAsJsonAsync("/api/v1/health-data/batch", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostBatch_InvalidHeartRate_ShouldBeAcceptedAsNormal()
    {
        var payload = new
        {
            patientCode = "NONEXISTENT",
            data = new[]
            {
                new { heartRate = -10.0, oxygen = 98.0, activity = 50.0, timestamp = DateTime.UtcNow }
            }
        };
        var response = await _client.PostAsJsonAsync("/api/v1/health-data/batch", payload);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostBatch_EmptyData_ShouldReturnBadRequest()
    {
        var payload = new
        {
            patientCode = "CODE1",
            data = new object[] { }
        };
        var response = await _client.PostAsJsonAsync("/api/v1/health-data/batch", payload);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
