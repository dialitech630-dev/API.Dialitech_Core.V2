using API.Dialitech.Domain.Entities;

namespace API.Dialitech.Application.Interfaces;

public interface ITokenService
{
    string GenerateToken(Caregiver caregiver);
}
