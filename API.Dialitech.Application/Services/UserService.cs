using API.Dialitech.Application.Commands.Users.CreateUser;
using API.Dialitech.Application.Commands.Users.DeleteUser;
using API.Dialitech.Application.Commands.Users.LoginUser;
using API.Dialitech.Application.Commands.Users.RegisterUser;
using API.Dialitech.Application.Commands.Users.UpdateUser;
using API.Dialitech.Application.DTOs;
using API.Dialitech.Application.Interfaces;
using API.Dialitech.Application.Queries.Users.GetAllUsers;
using API.Dialitech.Application.Queries.Users.GetUserById;
using MediatR;

namespace API.Dialitech.Application.Services;

public class UserService : IUserService
{
    private readonly IMediator _mediator;

    public UserService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IEnumerable<UserDto>> GetAllAsync()
    {
        return await _mediator.Send(new GetAllUsersQuery());
    }

    public async Task<UserDto?> GetByIdAsync(string id)
    {
        return await _mediator.Send(new GetUserByIdQuery(id));
    }

    public async Task CreateAsync(CreateUserDto dto)
    {
        await _mediator.Send(new CreateUserCommand(dto.Name, dto.Email, dto.Age));
    }

    public async Task UpdateAsync(string id, UpdateUserDto dto)
    {
        await _mediator.Send(new UpdateUserCommand(id, dto.Name, dto.Email, dto.Age));
    }

    public async Task DeleteAsync(string id)
    {
        await _mediator.Send(new DeleteUserCommand(id));
    }

    public async Task<UserDto> RegisterAsync(RegisterRequest request)
    {
        return await _mediator.Send(new RegisterUserCommand(request.Name, request.Email, request.Password, request.Age));
    }

    public async Task<UserDto?> LoginAsync(LoginRequest request)
    {
        return await _mediator.Send(new LoginUserCommand(request.Email, request.Password));
    }
}
