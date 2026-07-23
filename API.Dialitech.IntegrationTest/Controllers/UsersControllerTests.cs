using System.Net;
using System.Net.Http.Json;
using API.Dialitech.Application.DTOs;
using FluentAssertions;

namespace API.Dialitech.IntegrationTest.Controllers;

public class UsersControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UsersControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUsers_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateUser_ShouldReturnCreated()
    {
        var dto = new CreateUserDto
        {
            Name = "Test User",
            Email = $"test.{Guid.NewGuid()}@example.com",
            Age = 30
        };

        var response = await _client.PostAsJsonAsync("/api/users", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GetUserById_NonExistingId_ShouldReturnNotFound()
    {
        var response = await _client.GetAsync($"/api/users/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUserById_InvalidIdWithDollar_ShouldReturnBadRequest()
    {
        var response = await _client.GetAsync("/api/users/$ne=null");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
