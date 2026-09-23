using Berryfy.Application.Dtos.AuthDtos;
using Berryfy.Application.Dtos.AuthDtos.Requests;
using Berryfy.Domain.Entities.AuthEntities;

namespace Berryfy.Application.Services.Interfaces.AuthServiceInterfaces
{
    public interface IUserService
    {
        Task<bool> IsUserExistsByIdAsync(int userId);
        Task<bool> IsUserExistsByEmailAsync(string emailAddress);
        Task<List<User>> GetAllUsers();
        Task<User> GetUserById(int id);
        Task<User> GetUserByEmail(string email);
        Task<bool> LockUserAccountAsync(int userId, DateTime? lockoutEnd = null);
        Task<bool> UnlockUserAccountAsync(int userId);
        Task<bool> ResetUserPasswordAsync(int userId, string newPassword);
        Task<bool> VerifyUserEmailAsync(int userId);
        Task<bool> UpdateUserAsync(int userId, UpdateUserRequest updateUserDto);
        Task<User> CreateUserAsync(CreateUser createUserDto);
        Task<bool> DeleteUserAsync(int userId);
        Task<bool> IsUsernameTaken(string username);
    }
}
