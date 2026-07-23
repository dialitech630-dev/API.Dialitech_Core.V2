using MediatR;

namespace API.Dialitech.Application.Commands.Users.UpdateUser;

public record UpdateUserCommand(string Id, string Name, string Email, int Age) : IRequest;
