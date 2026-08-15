using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace API.Dialitech.SecurityTest;

public class PublicEndpointRateLimitTests : IClassFixture<SecurityWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly SecurityWebApplicationFactory _factory;

    public PublicEndpointRateLimitTests(SecurityWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ValidateCode_Flood_ShouldTriggerRateLimit()
    {
        var rateLimited = await FloodAsync(() =>
            _client.PostAsJsonAsync("/api/v1/patients/validate-code", new { code = "000000" }));

        rateLimited.Should().BeGreaterThan(0, "validate-code is public and must be rate limited");
    }

    [Fact]
    public async Task LinkDevice_Flood_ShouldTriggerRateLimit()
    {
        var rateLimited = await FloodAsync(() =>
            _client.PostAsJsonAsync("/api/v1/devices/link",
                new { code = "000000", serialNumber = $"SN-FLOOD-{Guid.NewGuid():N}" }));

        rateLimited.Should().BeGreaterThan(0, "devices/link is public and must be rate limited");
    }

    [Fact]
    public async Task PatientInfo_Flood_ShouldTriggerRateLimit()
    {
        var rateLimited = await FloodAsync(() =>
            _client.GetAsync("/api/v1/health-data/patient-info/000000"));

        rateLimited.Should().BeGreaterThan(0, "patient-info is public and must be rate limited");
    }

    [Fact]
    public async Task DeviceToken_Flood_ShouldTriggerRateLimit()
    {
        var rateLimited = await FloodAsync(() =>
            _client.PostAsJsonAsync("/api/v1/health-data/device-token",
                new { patientCode = "000000", deviceToken = "fcm-flood-test" }));

        rateLimited.Should().BeGreaterThan(0, "device-token is public and must be rate limited");
    }

    [Fact]
    public async Task Register_Flood_ShouldTriggerRateLimit()
    {
        var email = $"regflood.{Guid.NewGuid():N}";
        var rateLimited = await FloodAsync(() =>
            _client.PostAsJsonAsync("/api/v1/auth/register",
                new { name = "Flood", email = $"{email}.{Guid.NewGuid():N}@test.com", password = "Test123!", plan = "Standard" }));

        rateLimited.Should().BeGreaterThan(0, "register is public and must be rate limited");
    }

    [Fact]
    public async Task ForgotPassword_Flood_ShouldTriggerRateLimit()
    {
        var rateLimited = await FloodAsync(() =>
            _client.PostAsJsonAsync("/api/v1/auth/forgot-password",
                new { email = $"nobody.{Guid.NewGuid():N}@test.com" }));

        rateLimited.Should().BeGreaterThan(0, "forgot-password is public and must be rate limited");
    }

    [Fact]
    public async Task ResetPassword_Flood_ShouldTriggerRateLimit()
    {
        var rateLimited = await FloodAsync(() =>
            _client.PostAsJsonAsync("/api/v1/auth/reset-password",
                new { email = $"nobody.{Guid.NewGuid():N}@test.com", code = "000000", newPassword = "Test123!" }));

        rateLimited.Should().BeGreaterThan(0, "reset-password is public and must be rate limited");
    }

    private static async Task<int> FloodAsync(Func<Task<HttpResponseMessage>> requestFactory)
    {
        var tasks = Enumerable.Range(0, 60).Select(async _ =>
        {
            using var response = await requestFactory();
            return response.StatusCode == HttpStatusCode.TooManyRequests ? 1 : 0;
        });

        var results = await Task.WhenAll(tasks);
        return results.Sum();
    }
}