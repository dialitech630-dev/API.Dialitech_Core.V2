using API.Dialitech.Application.Commands.Users.CreateUser;
using API.Dialitech.Application.Commands.Users.DeleteUser;
using API.Dialitech.Application.Commands.Users.LoginUser;
using API.Dialitech.Application.Commands.Users.RegisterUser;
using API.Dialitech.Application.Commands.Users.UpdateUser;
using API.Dialitech.Application.DTOs;
using API.Dialitech.Application.Queries.Users.GetAllUsers;
using API.Dialitech.Application.Queries.Users.GetUserById;
using API.Dialitech.Application.Services;
using FluentAssertions;
using MediatR;
using Moq;

namespace API.Dialitech.UnitTest.Services;

public class UserServiceTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _service = new UserService(_mediatorMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldSendGetAllUsersQuery()
    {
        var users = new List<UserDto>
        {
            new() { Id = "1", Name = "Alice", Email = "alice@test.com" },
            new() { Id = "2", Name = "Bob", Email = "bob@test.com" }
        };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllUsersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var result = await _service.GetAllAsync();

        result.Should().HaveCount(2);
        result.First().Name.Should().Be("Alice");
        _mediatorMock.Verify(m => m.Send(It.IsAny<GetAllUsersQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoUsers_ShouldReturnEmpty()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllUsersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _service.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ShouldReturnUser()
    {
        var user = new UserDto { Id = "1", Name = "Alice", Email = "alice@test.com" };
        _mediatorMock.Setup(m => m.Send(It.Is<GetUserByIdQuery>(q => q.Id == "1"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _service.GetByIdAsync("1");

        result.Should().NotBeNull();
        result!.Name.Should().Be("Alice");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ShouldReturnNull()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetUserByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserDto?)null);

        var result = await _service.GetByIdAsync("99");

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldSendCreateUserCommand()
    {
        var dto = new CreateUserDto { Name = "Charlie", Email = "charlie@test.com", Age = 25 };

        await _service.CreateAsync(dto);

        _mediatorMock.Verify(m => m.Send(
            It.Is<CreateUserCommand>(c => c.Name == "Charlie" && c.Email == "charlie@test.com" && c.Age == 25),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldSendUpdateUserCommand()
    {
        var dto = new UpdateUserDto { Name = "New", Email = "new@test.com", Age = 30 };

        await _service.UpdateAsync("1", dto);

        _mediatorMock.Verify(m => m.Send(
            It.Is<UpdateUserCommand>(c => c.Id == "1" && c.Name == "New" && c.Age == 30),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldSendDeleteUserCommand()
    {
        await _service.DeleteAsync("1");

        _mediatorMock.Verify(m => m.Send(
            It.Is<DeleteUserCommand>(c => c.Id == "1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ShouldSendRegisterUserCommand()
    {
        var user = new UserDto { Id = "1", Name = "Dave", Email = "dave@test.com" };
        _mediatorMock.Setup(m => m.Send(It.IsAny<RegisterUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _service.RegisterAsync(new RegisterRequest
        {
            Name = "Dave",
            Email = "dave@test.com",
            Password = "secret",
            Age = 30
        });

        result.Name.Should().Be("Dave");
        _mediatorMock.Verify(m => m.Send(
            It.Is<RegisterUserCommand>(c => c.Name == "Dave" && c.Email == "dave@test.com" && c.Age == 30),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ShouldReturnUser()
    {
        var user = new UserDto { Id = "1", Name = "Eve", Email = "eve@test.com" };
        _mediatorMock.Setup(m => m.Send(It.IsAny<LoginUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _service.LoginAsync(new LoginRequest
        {
            Email = "eve@test.com",
            Password = "password"
        });

        result.Should().NotBeNull();
        result!.Email.Should().Be("eve@test.com");
    }

    [Fact]
    public async Task LoginAsync_InvalidCredentials_ShouldReturnNull()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<LoginUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserDto?)null);

        var result = await _service.LoginAsync(new LoginRequest
        {
            Email = "wrong@test.com",
            Password = "wrong"
        });

        result.Should().BeNull();
    }
}
