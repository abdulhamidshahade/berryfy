

using Berryfy.Domain.Entities.AuthEntities;

namespace Berryfy.Application.Services.Interfaces.AuthServiceInterfaces
{
    public interface ITokenService
    {
        Task<string> GenerateToken(User user);
        Task<string> GenerateRefreshToken(User user);
    }
}
