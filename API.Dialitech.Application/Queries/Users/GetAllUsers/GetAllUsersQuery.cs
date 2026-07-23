using API.Dialitech.Application.DTOs;
using MediatR;

namespace API.Dialitech.Application.Queries.Users.GetAllUsers;

public record GetAllUsersQuery : IRequest<IEnumerable<UserDto>>;
