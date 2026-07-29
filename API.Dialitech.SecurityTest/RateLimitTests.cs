using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace API.Dialitech.SecurityTest;

public class RateLimitTests : IClassFixture<SecurityWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RateLimitTests(SecurityWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task LoginEndpoint_AfterMultipleAttempts_ShouldNotRateLimitLegitimate()
    {
        var email = $"rl.{Guid.NewGuid()}@test.com";
        var registerPayload = new { name = "Rate Limit Test", email, password = "Test123!", plan = "Standard" };
        await _client.PostAsJsonAsync("/api/v1/auth/register", registerPayload);

        var loginPayload = new { email, password = "Test123!" };

        // First few attempts should succeed
        for (int i = 0; i < 3; i++)
        {
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginPayload);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task BatchEndpoint_ShouldRejectAfterExceedingLimit()
    {
        var payload = new
        {
            patientCode = "TEST",
            data = new[]
            {
                new { heartRate = 75.0, oxygen = 98.0, activity = 50.0, timestamp = DateTime.UtcNow }
            }
        };

        int okCount = 0;
        int tooManyCount = 0;

        // Send many rapid requests to trigger rate limiting
        var tasks = Enumerable.Range(0, 50).Select(async _ =>
        {
            var response = await _client.PostAsJsonAsync("/api/v1/health-data/batch", payload);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                Interlocked.Increment(ref tooManyCount);
            else
                Interlocked.Increment(ref okCount);
        });

        await Task.WhenAll(tasks);

        tooManyCount.Should().BeGreaterThan(0, "Rate limiting should be triggered");
    }
}
