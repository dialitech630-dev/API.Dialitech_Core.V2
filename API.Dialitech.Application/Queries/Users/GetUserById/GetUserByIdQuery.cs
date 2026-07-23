using API.Dialitech.Application.DTOs;
using MediatR;

namespace API.Dialitech.Application.Queries.Users.GetUserById;

public record GetUserByIdQuery(string Id) : IRequest<UserDto?>;
