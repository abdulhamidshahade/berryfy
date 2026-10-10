using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.AuthDtos;
using Berryfy.Application.Dtos.AuthDtos.Requests;
using Berryfy.Domain.Entities.AuthEntities;

namespace Berryfy.Application.Services.Interfaces.AuthServiceInterfaces
{
    public interface IUserService
    {
        Task<ApplicationResponse<bool>> IsUserExistsByIdAsync(int userId);
        Task<ApplicationResponse<bool>> IsUserExistsByEmailAsync(string emailAddress);
        Task<ApplicationResponse<List<User>>> GetAllUsers();
        Task<ApplicationResponse<User>> GetUserById(int id);
        Task<ApplicationResponse<User>> GetUserByEmail(string email);
        Task<ApplicationResponse<bool>> LockUserAccountAsync(int userId, DateTime? lockoutEnd = null);
        Task<ApplicationResponse<bool>> UnlockUserAccountAsync(int userId);
        Task<ApplicationResponse<bool>> ResetUserPasswordAsync(int userId, string newPassword);
        Task<ApplicationResponse<bool>> VerifyUserEmailAsync(int userId);
        Task<ApplicationResponse<bool>> UpdateUserAsync(int userId, UpdateUserRequest updateUserDto);
        Task<ApplicationResponse<User>> CreateUserAsync(CreateUser createUserDto);
        Task<ApplicationResponse<bool>> DeleteUserAsync(int userId);
        Task<ApplicationResponse<bool>> IsUsernameTaken(string username);
        Task<ApplicationResponse<bool>> RevokeRefreshTokenAsync(int userId);
    }
}
