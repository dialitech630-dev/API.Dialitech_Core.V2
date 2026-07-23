using API.Dialitech.Application.DTOs;
using MediatR;

namespace API.Dialitech.Application.Commands.Users.RegisterUser;

public record RegisterUserCommand(string Name, string Email, string Password, int Age) : IRequest<UserDto>;
