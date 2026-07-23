using MediatR;

namespace API.Dialitech.Application.Commands.Users.DeleteUser;

public record DeleteUserCommand(string Id) : IRequest;
