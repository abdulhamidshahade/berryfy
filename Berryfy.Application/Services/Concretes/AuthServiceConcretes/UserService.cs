using Berryfy.Application.Dtos;
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

        public async Task<ApplicationResponse<List<User>>> GetAllUsers()
        {
            return new ApplicationResponse<List<User>>
            {
                IsSuccess = true,
                SuccessMessage = "Users retrieved successfully",
                Value = _userRepository.GetAllAsync().GetAwaiter().GetResult().Value
            };
        }

        public async Task<ApplicationResponse<User>> GetUserById(int id)
        {
            var user = _userRepository.GetByIdAsync(id).GetAwaiter().GetResult().Value;
            if (user == null)
            {
                return new ApplicationResponse<User>
                {
                    IsSuccess = false,
                    ErrorMessage = "User not found"
                };
            }

            return new ApplicationResponse<User>
            {
                IsSuccess = true,
                SuccessMessage = "User retrieved successfully",
                Value = user
            };
        }

        public async Task<ApplicationResponse<bool>> IsUserExistsByEmailAsync(string emailAddress)
        {
            return new ApplicationResponse<bool>
            {
                IsSuccess = true,
                SuccessMessage = "User exists",
                Value = _userRepository.ExistsByEmailAsync(EmailNormalizer.NormalizeEmail(emailAddress)).GetAwaiter().GetResult().Value
            };
        }

        public async Task<ApplicationResponse<bool>> IsUserExistsByIdAsync(int userId)
        {
            return new ApplicationResponse<bool>
            {
                IsSuccess = true,
                SuccessMessage = "User exists",
                Value = _userRepository.ExistsByIdAsync(userId).GetAwaiter().GetResult().Value
            };
        }

        public async Task<ApplicationResponse<bool>> LockUserAccountAsync(int userId, DateTime? lockoutEnd = null)
        {
            try
            {
                if (!_userRepository.ExistsByIdAsync(userId).GetAwaiter().GetResult().Value)
                {
                    _logger.LogWarning("User with ID {UserId} not found for lock operation", userId);
                    return new ApplicationResponse<bool>
                    {
                        IsSuccess = false,
                        ErrorMessage = "User not found"
                    };
                }

                return new ApplicationResponse<bool>
                {
                    IsSuccess = true,
                    SuccessMessage = "User account locked successfully",
                    Value = _userRepository.SetLockoutAsync(userId, lockoutEnd ?? DateTime.UtcNow.AddYears(100)).GetAwaiter().GetResult().Value
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error locking user with ID {UserId}", userId);
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = "Error locking user account"
                };
            }
        }

        public async Task<ApplicationResponse<bool>> UnlockUserAccountAsync(int userId)
        {
            try
            {
                if (!_userRepository.SetLockoutAsync(userId, null).GetAwaiter().GetResult().Value)
                {
                    return new ApplicationResponse<bool>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Error unlocking user account"
                    };
                }

                await _userRepository.ResetAccessFailedCountAsync(userId);
                return new ApplicationResponse<bool>
                {
                    IsSuccess = true,
                    SuccessMessage = "User account unlocked successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlocking user with ID {UserId}", userId);
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = "Error unlocking user account"
                };
            }
        }

        public async Task<ApplicationResponse<bool>> ResetUserPasswordAsync(int userId, string newPassword)
        {
            try
            {
                var user = _userRepository.GetByIdAsync(userId).GetAwaiter().GetResult().Value;
                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found for password reset", userId);
                    return new ApplicationResponse<bool>
                    {
                        IsSuccess = false,
                        ErrorMessage = "User not found"
                    };
                }

                var passwordHash = _passwordHasher.HashPassword(user, newPassword);
                return new ApplicationResponse<bool>
                {
                    IsSuccess = true,
                    SuccessMessage = "Password reset successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user with ID {UserId}", userId);
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = "Error resetting password"
                };
            }
        }

        public async Task<ApplicationResponse<bool>> VerifyUserEmailAsync(int userId)
        {
            var result = _userRepository.ConfirmEmailAsync(userId).GetAwaiter().GetResult().Value;
            return new ApplicationResponse<bool>
            {
                IsSuccess = result,
                SuccessMessage = "Email verified successfully"
            };
        }

        public async Task<ApplicationResponse<bool>> UpdateUserAsync(int userId, UpdateUserRequest updateUserDto)
        {
            try
            {
                var user = _userRepository.GetByIdAsync(userId).GetAwaiter().GetResult().Value;
                if (user == null)
                {
                    return new ApplicationResponse<bool>
                    {
                        IsSuccess = false,
                        ErrorMessage = "User not found"
                    };
                }

                user.Email = updateUserDto.Email.Trim();
                user.NormalizedEmail = EmailNormalizer.NormalizeEmail(updateUserDto.Email);
                user.UserName = updateUserDto.UserName.Trim();
                user.NormalizedUserName = NormalizeName(updateUserDto.UserName);
                user.FirstName = updateUserDto.FirstName ?? string.Empty;
                user.LastName = updateUserDto.LastName ?? string.Empty;
                user.EmailConfirmed = updateUserDto.EmailConfirmed;
                user.ConcurrencyStamp = Guid.NewGuid().ToString();

                var result = _userRepository.UpdateAsync(user).GetAwaiter().GetResult().Value;
                return new ApplicationResponse<bool>
                {
                    IsSuccess = result,
                    SuccessMessage = "User updated successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user with ID {UserId}", userId);
                return new ApplicationResponse<bool>
                {
                    IsSuccess = false,
                    ErrorMessage = "Error updating user"
                };
            }
        }

        public async Task<ApplicationResponse<User>> CreateUserAsync(CreateUser createUserDto)
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

                if (IsUsernameTaken(user.NormalizedUserName).GetAwaiter().GetResult().Value)
                {
                    return new ApplicationResponse<User>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Username is already taken"
                    };
                }

                if(IsUserExistsByEmailAsync(user.NormalizedEmail).GetAwaiter().GetResult().Value)
                {
                    return new ApplicationResponse<User>
                    {
                        IsSuccess = false,
                        ErrorMessage = "Email is already registered"
                    };
                }

                user.PasswordHash = _passwordHasher.HashPassword(user, createUserDto.Password);

                user = _userRepository.CreateAsync(user).GetAwaiter().GetResult().Value;

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
                return new ApplicationResponse<User>
                {
                    IsSuccess = true,
                    SuccessMessage = "User created successfully",
                    Value = user
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user {UserName}", createUserDto.UserName);
                return new ApplicationResponse<User>
                {
                    IsSuccess = false,
                    ErrorMessage = "Error creating user"
                };
            }
        }

        public async Task<ApplicationResponse<bool>> DeleteUserAsync(int userId)
        {
            return new ApplicationResponse<bool>
            {
                IsSuccess = _userRepository.DeleteAsync(userId).GetAwaiter().GetResult().Value,
                SuccessMessage = "User deleted successfully"
            };
        }

        public async Task<ApplicationResponse<User>> GetUserByEmail(string email)
        {
            return new ApplicationResponse<User>
            {
                IsSuccess = true,
                Value = _userRepository.GetByNormalizedEmailAsync(EmailNormalizer.NormalizeEmail(email)).GetAwaiter().GetResult().Value
                    ?? _userRepository.GetByEmailAsync(email).GetAwaiter().GetResult().Value
            };
        }

        public async Task<ApplicationResponse<bool>> IsUsernameTaken(string username)
        {
            return new ApplicationResponse<bool>
            {
                IsSuccess = true,
                Value = _userRepository.IsUsernameTakenAsync(username).GetAwaiter().GetResult().Value
            };
        }

        private static string NormalizeName(string value)
        {
            return value.Trim().ToUpperInvariant();
        }

        public async Task<ApplicationResponse<bool>> RevokeRefreshTokenAsync(int userId)
        {
            var revoked = _userRepository.RevokeRefreshTokenAsync(userId).GetAwaiter().GetResult().Value;

            return new ApplicationResponse<bool>()
            {
                IsSuccess = revoked,
                SuccessMessage = revoked ? "Refresh token revoked successfully" : "Failed to revoke refresh token",
                Value = revoked
            };
        }
    }
}
