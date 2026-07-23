using API.Dialitech.Application.Common.Exceptions;
using API.Dialitech.Domain.Interfaces;
using MediatR;

namespace API.Dialitech.Application.Commands.Users.UpdateUser;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand>
{
    private readonly IUserRepository _userRepository;

    public UpdateUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id);
        if (user is null)
            throw new NotFoundException(nameof(Domain.Entities.User), request.Id);

        user.Name = request.Name;
        user.Email = request.Email;

        await _userRepository.UpdateAsync(user);
    }
}
