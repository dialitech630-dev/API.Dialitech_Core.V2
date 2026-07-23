using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace API.Dialitech.IntegrationTest.Controllers;

public class WeatherForecastControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public WeatherForecastControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetWeatherForecast_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/WeatherForecast");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetWeatherForecast_ShouldReturnArrayOfForecasts()
    {
        var forecasts = await _client.GetFromJsonAsync<List<object>>("/WeatherForecast");

        forecasts.Should().NotBeNull();
        forecasts.Should().HaveCount(5);
    }
}
