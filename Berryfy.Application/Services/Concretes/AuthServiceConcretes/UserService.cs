using Berryfy.Application.Dtos.AuthDtos.Requests;
using Berryfy.Application.Halpers;
using Berryfy.Application.Services.Interfaces.AuthServiceInterfaces;
using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities.AuthEntities;
using Berryfy.Domain.Repositories.AuthInterfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Berryfy.Application.Services.Concretes.AuthServiceConcretes
{
    public class UserService : IUserService
    {
        private readonly ILogger<UserService> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IPasswordHasher<User> _passwordHasher;

        public UserService(
            ILogger<UserService> logger,
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IPasswordHasher<User> passwordHasher)
        {
            _logger = logger;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _passwordHasher = passwordHasher;
        }

        public Task<List<User>> GetAllUsers()
        {
            return _userRepository.GetAllAsync();
        }

        public async Task<User> GetUserById(int id)
        {
            return await _userRepository.GetByIdAsync(id);
        }

        public Task<bool> IsUserExistsByEmailAsync(string emailAddress)
        {
            return _userRepository.ExistsByEmailAsync(EmailNormalizer.NormalizeEmail(emailAddress));
        }

        public Task<bool> IsUserExistsByIdAsync(int userId)
        {
            return _userRepository.ExistsByIdAsync(userId);
        }

        public async Task<bool> LockUserAccountAsync(int userId, DateTime? lockoutEnd = null)
        {
            try
            {
                if (!await _userRepository.ExistsByIdAsync(userId))
                {
                    _logger.LogWarning("User with ID {UserId} not found for lock operation", userId);
                    return false;
                }

                return await _userRepository.SetLockoutAsync(userId, lockoutEnd ?? DateTime.UtcNow.AddYears(100));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error locking user with ID {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> UnlockUserAccountAsync(int userId)
        {
            try
            {
                if (!await _userRepository.SetLockoutAsync(userId, null))
                {
                    return false;
                }

                await _userRepository.ResetAccessFailedCountAsync(userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlocking user with ID {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> ResetUserPasswordAsync(int userId, string newPassword)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found for password reset", userId);
                    return false;
                }

                var passwordHash = _passwordHasher.HashPassword(user, newPassword);
                return await _userRepository.UpdatePasswordHashAsync(user.Id, passwordHash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user with ID {UserId}", userId);
                return false;
            }
        }

        public Task<bool> VerifyUserEmailAsync(int userId)
        {
            return _userRepository.ConfirmEmailAsync(userId);
        }

        public async Task<bool> UpdateUserAsync(int userId, UpdateUserRequest updateUserDto)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return false;
                }

                user.Email = updateUserDto.Email.Trim();
                user.NormalizedEmail = EmailNormalizer.NormalizeEmail(updateUserDto.Email);
                user.UserName = updateUserDto.UserName.Trim();
                user.NormalizedUserName = NormalizeName(updateUserDto.UserName);
                user.FirstName = updateUserDto.FirstName ?? string.Empty;
                user.LastName = updateUserDto.LastName ?? string.Empty;
                user.EmailConfirmed = updateUserDto.EmailConfirmed;
                user.ConcurrencyStamp = Guid.NewGuid().ToString();

                return await _userRepository.UpdateAsync(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID {UserId}", userId);
                return false;
            }
        }

        public async Task<User> CreateUserAsync(CreateUser createUserDto)
        {
            try
            {
                var user = new User
                {
                    Email = createUserDto.Email.Trim(),
                    NormalizedEmail = EmailNormalizer.NormalizeEmail(createUserDto.Email),
                    UserName = createUserDto.UserName.Trim(),
                    NormalizedUserName = NormalizeName(createUserDto.UserName),
                    FirstName = createUserDto.FirstName ?? string.Empty,
                    LastName = createUserDto.LastName ?? string.Empty,
                    EmailConfirmed = createUserDto.EmailConfirmed,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                };
                user.PasswordHash = _passwordHasher.HashPassword(user, createUserDto.Password);

                user = await _userRepository.CreateAsync(user);

                var roles = createUserDto.Roles.Any() ? createUserDto.Roles : new List<string> { RoleConstants.User };
                foreach (var role in roles)
                {
                    if (!_roleRepository.RoleExistsAsync(role).GetAwaiter().GetResult().Value)
                    {
                        await _roleRepository.CreateAsync(new Role(role));
                    }

                    await _roleRepository.AssignRoleToUserAsync(user.Id, role);
                }

                _logger.LogInformation("User {UserName} created successfully", user.UserName);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user {UserName}", createUserDto.UserName);
                return null;
            }
        }

        public Task<bool> DeleteUserAsync(int userId)
        {
            return _userRepository.DeleteAsync(userId);
        }

        public async Task<User> GetUserByEmail(string email)
        {
            return await _userRepository.GetByNormalizedEmailAsync(EmailNormalizer.NormalizeEmail(email))
                ?? await _userRepository.GetByEmailAsync(email);
        }

        public async Task<bool> IsUsernameTaken(string username)
        {
            return await _userRepository.IsUsernameTakenAsync(username);
        }

        private static string NormalizeName(string value)
        {
            return value.Trim().ToUpperInvariant();
        }
    }
}
