using Berryfy.Domain.Entities.AuthEntities;

namespace Berryfy.Domain.Repositories.AuthInterfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByNormalizedEmailAsync(string normalizedEmail);
        Task<User?> GetByRefreshTokenAsync(string refreshTokenHash);
        Task<List<User>> GetAllAsync();
        Task<User> CreateAsync(User user);
        Task<bool> UpdateAsync(User user);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsByIdAsync(int id);
        Task<bool> ExistsByEmailAsync(string email);
        Task<bool> IsUsernameTakenAsync(string userName);
        Task<bool> SetLockoutAsync(int userId, DateTime? lockoutEnd);
        Task<bool> ResetAccessFailedCountAsync(int userId);
        Task<bool> IncrementAccessFailedCountAsync(int userId);
        Task<bool> UpdatePasswordHashAsync(int userId, string passwordHash);
        Task<bool> ConfirmEmailAsync(int userId);
    }
}
