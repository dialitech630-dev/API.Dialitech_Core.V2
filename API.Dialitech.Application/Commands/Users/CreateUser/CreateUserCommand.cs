using MediatR;

namespace API.Dialitech.Application.Commands.Users.CreateUser;

public record CreateUserCommand(string Name, string Email, int Age) : IRequest;
