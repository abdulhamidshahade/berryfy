using AutoMapper;
using Berryfy.Application.Dtos.AuthDtos;
using Berryfy.Application.Services.Interfaces.AuthServiceInterfaces;
using Berryfy.Domain.Entities.AuthEntities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Berryfy.Application.Services.Concretes.AuthServiceConcretes
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly ILogger<UserService> _logger;

        public UserService(UserManager<ApplicationUser> userManager,
            IMapper mapper,
            RoleManager<ApplicationRole> roleManager,
            ILogger<UserService> logger)
        {
            _userManager = userManager;
            _mapper = mapper;
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task<List<ApplicationUserDto>> GetAllUsers()
        {
            var users = await _userManager.Users.ToListAsync();

            foreach (var user in users)
            {
                user.roles = await _userManager.GetRolesAsync(user);
            }

            return _mapper.Map<List<ApplicationUserDto>>(users);
        }

        public async Task<ApplicationUserDto> GetUserById(int id)
        {
            var user = await _userManager.Users.Where(i => i.Id == id).FirstOrDefaultAsync();
            if (user != null) user.roles = await _userManager.GetRolesAsync(user);

            return _mapper.Map<ApplicationUserDto>(user);
        }

        public async Task<bool> IsUserExistsByEmailAsync(string emailAddress)
        {
            var user = await _userManager.FindByEmailAsync(emailAddress);

            return user != null;
        }

        public async Task<bool> IsUserExistsByIdAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            return user != null;
        }

        public async Task<bool> LockUserAccountAsync(int userId, DateTime? lockoutEnd = null)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    _logger.LogWarning($"User with ID {userId} not found for lock operation");
                    return false;
                }

                var lockoutEndDate = lockoutEnd ?? DateTimeOffset.UtcNow.AddYears(100);

                var result = await _userManager.SetLockoutEndDateAsync(user, lockoutEndDate);
                if (result.Succeeded)
                {
                    await _userManager.SetLockoutEnabledAsync(user, true);
                    _logger.LogInformation($"User {user.UserName} locked successfully");
                    return true;
                }

                _logger.LogError($"Failed to lock user {user.UserName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error locking user with ID {userId}");
                return false;
            }
        }

        public async Task<bool> UnlockUserAccountAsync(int userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    _logger.LogWarning($"User with ID {userId} not found for unlock operation");
                    return false;
                }

                var result = await _userManager.SetLockoutEndDateAsync(user, null);
                if (result.Succeeded)
                {
                    await _userManager.ResetAccessFailedCountAsync(user);
                    _logger.LogInformation($"User {user.UserName} unlocked successfully");
                    return true;
                }

                _logger.LogError($"Failed to unlock user {user.UserName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error unlocking user with ID {userId}");
                return false;
            }
        }

        public async Task<bool> ResetUserPasswordAsync(int userId, string newPassword)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    _logger.LogWarning($"User with ID {userId} not found for password reset");
                    return false;
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                var result = await PasswordSessionRevocation.ExecuteAsync(user,
                    () => _userManager.ResetPasswordAsync(user, token, newPassword));
                if (result.Succeeded)
                {
                    _logger.LogInformation($"Password reset successfully for user {user.UserName}");
                    return true;
                }

                _logger.LogError($"Failed to reset password for user {user.UserName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error resetting password for user with ID {userId}");
                return false;
            }
        }

        public async Task<bool> VerifyUserEmailAsync(int userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    _logger.LogWarning($"User with ID {userId} not found for email verification");
                    return false;
                }

                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                var result = await _userManager.ConfirmEmailAsync(user, token);
                if (result.Succeeded)
                {
                    _logger.LogInformation($"Email verified successfully for user {user.UserName}");
                    return true;
                }

                _logger.LogError($"Failed to verify email for user {user.UserName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error verifying email for user with ID {userId}");
                return false;
            }
        }

        public async Task<bool> UpdateUserAsync(int userId, UpdateUserDto updateUserDto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    _logger.LogWarning($"User with ID {userId} not found for update");
                    return false;
                }

                user.Email = updateUserDto.Email;
                user.UserName = updateUserDto.UserName;
                user.FirstName = updateUserDto.FirstName;
                user.LastName = updateUserDto.LastName;
                user.EmailConfirmed = updateUserDto.EmailConfirmed;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation($"User {user.UserName} updated successfully");
                    return true;
                }

                _logger.LogError($"Failed to update user {user.UserName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating user with ID {userId}");
                return false;
            }
        }

        public async Task<ApplicationUserDto> CreateUserAsync(CreateUserDto createUserDto)
        {
            try
            {
                var user = new ApplicationUser
                {
                    Email = createUserDto.Email,
                    UserName = createUserDto.UserName,
                    FirstName = createUserDto.FirstName,
                    LastName = createUserDto.LastName,
                    EmailConfirmed = createUserDto.EmailConfirmed
                };

                var result = await _userManager.CreateAsync(user, createUserDto.Password);
                if (result.Succeeded)
                {
                    foreach (var role in createUserDto.Roles)
                    {
                        if (await _roleManager.RoleExistsAsync(role))
                        {
                            await _userManager.AddToRoleAsync(user, role);
                        }
                    }

                    _logger.LogInformation($"User {user.UserName} created successfully");

                    var roles = await _userManager.GetRolesAsync(user);
                    user.roles = roles;

                    return _mapper.Map<ApplicationUserDto>(user);
                }

                _logger.LogError($"Failed to create user {createUserDto.UserName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating user {createUserDto.UserName}");
                return null;
            }
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    _logger.LogWarning($"User with ID {userId} not found for deletion");
                    return false;
                }

                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation($"User {user.UserName} deleted successfully");
                    return true;
                }

                _logger.LogError($"Failed to delete user {user.UserName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting user with ID {userId}");
                return false;
            }
        }

        public async Task<ApplicationUserDto> GetUserByEmail(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            return _mapper.Map<ApplicationUserDto>(user);
        }

        public async Task<bool> IsUsernameTaken(string username)
        {
            return await _userManager.Users.AnyAsync(u => u.UserName == username);
        }
    }
}
