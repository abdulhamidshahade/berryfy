using Berryfy.Domain.Entities;
using Berryfy.Domain.Entities.AuthEntities;

namespace Berryfy.Domain.Repositories.AuthInterfaces
{
    public interface IUserRepository
    {
        Task<InfrastructureResponse<User?>> GetByIdAsync(int id);
        Task<InfrastructureResponse<User?>> GetByEmailAsync(string email);
        Task<InfrastructureResponse<User?>> GetByNormalizedEmailAsync(string normalizedEmail);
        Task<InfrastructureResponse<User?>> GetByRefreshTokenAsync(string refreshTokenHash);
        Task<InfrastructureResponse<List<User>>> GetAllAsync();
        Task<InfrastructureResponse<User>> CreateAsync(User user);
        Task<InfrastructureResponse<bool>> UpdateAsync(User user);
        Task<InfrastructureResponse<bool>> DeleteAsync(int id);
        Task<InfrastructureResponse<bool>> ExistsByIdAsync(int id);
        Task<InfrastructureResponse<bool>> ExistsByEmailAsync(string email);
        Task<InfrastructureResponse<bool>> IsUsernameTakenAsync(string userName);
        Task<InfrastructureResponse<bool>> SetLockoutAsync(int userId, DateTime? lockoutEnd);
        Task<InfrastructureResponse<bool>> ResetAccessFailedCountAsync(int userId);
        Task<InfrastructureResponse<bool>> IncrementAccessFailedCountAsync(int userId);
        Task<InfrastructureResponse<bool>> UpdatePasswordHashAsync(int userId, string passwordHash);
        Task<InfrastructureResponse<bool>> ConfirmEmailAsync(int userId);
        Task<InfrastructureResponse<bool>> UpdateLockoutStateAsync(int userId);
    }
}
