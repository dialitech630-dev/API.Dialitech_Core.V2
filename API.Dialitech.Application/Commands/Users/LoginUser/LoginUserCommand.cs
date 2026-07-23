using API.Dialitech.Application.DTOs;
using MediatR;

namespace API.Dialitech.Application.Commands.Users.LoginUser;

public record LoginUserCommand(string Email, string Password) : IRequest<UserDto?>;
