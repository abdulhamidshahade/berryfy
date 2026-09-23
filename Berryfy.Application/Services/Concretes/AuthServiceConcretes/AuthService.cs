using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.AuthDtos.Requests;
using Berryfy.Application.Dtos.AuthDtos.Responses;
using Berryfy.Application.Halpers;
using Berryfy.Application.Services.Interfaces.AuthServiceInterfaces;
using Berryfy.Application.Services.Interfaces.EmailServiceInterfaces;
using Berryfy.Domain.Constants;
using Berryfy.Domain.Entities.AuthEntities;
using Berryfy.Domain.Repositories.AuthInterfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace Berryfy.Application.Services.Concretes.AuthServiceConcretes
{
    public class AuthService : IAuthService
    {
        private readonly ILogger<AuthService> _logger;
        private readonly ITokenService _tokenService;
        private readonly IRoleManagementService _roleService;
        private readonly IMailService _mailService;
        private readonly IUserService _userService;
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AuthService(
            ILogger<AuthService> logger,
            ITokenService tokenService,
            IRoleManagementService roleService,
            IMailService mailService,
            IUserService userService,
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IPasswordHasher<User> passwordHasher)
        {
            _logger = logger;
            _tokenService = tokenService;
            _roleService = roleService;
            _mailService = mailService;
            _userService = userService;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task<Result<RegisterResponse>> Register(RegisterRequest requestDto)
        {
            if (requestDto == null)
            {
                return Result<RegisterResponse>.ValidationError("Registration data is required.");
            }

            if (await _userService.IsUsernameTaken(requestDto.UserName))
            {
                return Result<RegisterResponse>.ValidationError("Username is taken.");
            }

            if (await _userService.IsUserExistsByEmailAsync(requestDto.Email))
            {
                return Result<RegisterResponse>.ValidationError("Email is already registered.");
            }

            try
            {
                var normalizedEmail = EmailNormalizer.NormalizeEmail(requestDto.Email);
                var user = new User
                {
                    FirstName = requestDto.FirstName,
                    LastName = requestDto.LastName,
                    UserName = requestDto.UserName,
                    NormalizedUserName = NormalizeName(requestDto.UserName),
                    Email = requestDto.Email.Trim(),
                    NormalizedEmail = normalizedEmail,
                    EmailConfirmed = false,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                };
                user.PasswordHash = _passwordHasher.HashPassword(user, requestDto.Password);

                user = await _userRepository.CreateAsync(user);

                if (!await _roleService.AssignRoleToUserAsync(user.Id, RoleConstants.User))
                {
                    return Result<RegisterResponse>.Failure("User created but failed to assign default role.");
                }

                var code = GenerateOtpCode();
                user.EmailConfirmationCode = code;
                user.EmailConfirmationCodeExpiry = DateTime.UtcNow.AddMinutes(15);
                await _userRepository.UpdateAsync(user);

                try
                {
                    await _mailService.SendEmailConfirmationAsync(user.Email, code, user.UserName);
                    _logger.LogInformation("Email confirmation code sent successfully to {Email}", user.Email);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send email confirmation to user {Email}", user.Email);
                }

                var roles = await _roleRepository.GetUserRolesAsync(user.Id);
                return Result<RegisterResponse>.Success(new RegisterResponse
                {
                    IsSuccess = true,
                    User = UserResponse.MapFromUser(user, roles)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while registering the user.");
                return Result<RegisterResponse>.Failure("An unexpected error occurred during registration. Please try again later.");
            }
        }

        public async Task<LoginResponse> Login(LoginRequest requestDto)
        {
            var normalizedEmail = EmailNormalizer.NormalizeEmail(requestDto.Email);
            var user = await _userRepository.GetByNormalizedEmailAsync(normalizedEmail)
                ?? await _userRepository.GetByEmailAsync(requestDto.Email);

            if (user == null)
            {
                _logger.LogWarning("User not found for email: {Email}", normalizedEmail);
                return new LoginResponse { User = null, Token = string.Empty };
            }

            if (IsLockedOut(user))
            {
                return new LoginResponse { Token = string.Empty, ErrorMessage = "Account is locked. Please try again later." };
            }

            var passwordResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, requestDto.Password);
            if (passwordResult == PasswordVerificationResult.Failed)
            {
                await _userRepository.IncrementAccessFailedCountAsync(user.Id);
                _logger.LogWarning("Invalid password for email: {Email}", normalizedEmail);
                return new LoginResponse { User = null, Token = string.Empty };
            }

            if (!user.EmailConfirmed)
            {
                _logger.LogWarning("Login attempted with unconfirmed email: {Email}", normalizedEmail);
                return new LoginResponse
                {
                    User = null,
                    Token = string.Empty,
                    ErrorMessage = "Email not confirmed. Please check your email and confirm your account."
                };
            }

            await _userRepository.ResetAccessFailedCountAsync(user.Id);
            var roles = await _roleRepository.GetUserRolesAsync(user.Id);
            var token = await _tokenService.GenerateToken(user);
            var refreshToken = await _tokenService.GenerateRefreshToken(user);

            return new LoginResponse
            {
                User = UserResponse.MapFromUser(user, roles),
                Token = token,
                RefreshToken = refreshToken
            };
        }

        public async Task<LoginResponse> RefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return new LoginResponse { Token = string.Empty, ErrorMessage = "Invalid or expired refresh token." };
            }

            var hashedToken = TokenService.HashToken(refreshToken);
            var user = await _userRepository.GetByRefreshTokenAsync(hashedToken);

            if (user == null ||
                user.RefreshTokenExpiry == null ||
                user.RefreshTokenExpiry <= DateTime.UtcNow ||
                !user.EmailConfirmed ||
                IsLockedOut(user))
            {
                return new LoginResponse { Token = string.Empty, ErrorMessage = "Invalid or expired refresh token." };
            }

            var roles = await _roleRepository.GetUserRolesAsync(user.Id);
            return new LoginResponse
            {
                User = UserResponse.MapFromUser(user, roles),
                Token = await _tokenService.GenerateToken(user),
                RefreshToken = await _tokenService.GenerateRefreshToken(user)
            };
        }

        public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequest requestDto)
        {
            try
            {
                var user = await FindUserByEmailAsync(requestDto.Email);
                if (user == null)
                {
                    _logger.LogWarning("Password reset requested for non-existent email: {Email}", requestDto.Email);
                    return true;
                }

                await SetPasswordResetCodeAsync(user);
                await SendPasswordResetCodeAsync(user);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing forgot password request for email: {Email}", requestDto.Email);
                return false;
            }
        }

        public async Task<VerifyPasswordResetCodeResponse?> VerifyPasswordResetCodeAsync(EmailConfirmation confirmationDto)
        {
            try
            {
                var user = await FindUserByEmailAsync(confirmationDto.Email);
                if (user == null || user.PasswordResetCode == null || user.PasswordResetCodeExpiry == null)
                {
                    return null;
                }

                if (DateTime.UtcNow > user.PasswordResetCodeExpiry || user.PasswordResetCode != confirmationDto.Code)
                {
                    return null;
                }

                var resetToken = GenerateResetToken();
                user.PasswordResetCode = TokenService.HashToken(resetToken);
                user.PasswordResetCodeExpiry = DateTime.UtcNow.AddMinutes(15);
                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("Password reset code verified for user {Email}", user.Email);
                return new VerifyPasswordResetCodeResponse { ResetToken = resetToken };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while verifying password reset code for: {Email}", confirmationDto.Email);
                return null;
            }
        }

        public async Task<bool> ResendPasswordResetCodeAsync(string email)
        {
            try
            {
                var user = await FindUserByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("Resend password reset requested for non-existent email: {Email}", email);
                    return true;
                }

                await SetPasswordResetCodeAsync(user);
                await SendPasswordResetCodeAsync(user);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while resending password reset code for: {Email}", email);
                return false;
            }
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordRequest requestDto)
        {
            try
            {
                if (requestDto.NewPassword != requestDto.ConfirmPassword)
                {
                    return false;
                }

                var user = await FindUserByEmailAsync(requestDto.Email);
                if (user == null || user.PasswordResetCode == null || user.PasswordResetCodeExpiry == null)
                {
                    return false;
                }

                var tokenHash = TokenService.HashToken(requestDto.Token);
                if (DateTime.UtcNow > user.PasswordResetCodeExpiry || user.PasswordResetCode != tokenHash)
                {
                    return false;
                }

                var passwordHash = _passwordHasher.HashPassword(user, requestDto.NewPassword);
                var result = await _userRepository.UpdatePasswordHashAsync(user.Id, passwordHash);

                if (result)
                {
                    _logger.LogInformation("Password reset successful for user {Email}", user.Email);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while resetting password for email: {Email}", requestDto.Email);
                return false;
            }
        }

        public async Task<bool> ConfirmEmailAsync(EmailConfirmation confirmationDto)
        {
            try
            {
                var user = await FindUserByEmailAsync(confirmationDto.Email);
                if (user == null)
                {
                    return false;
                }

                if (user.EmailConfirmed)
                {
                    return true;
                }

                if (user.EmailConfirmationCode == null ||
                    user.EmailConfirmationCodeExpiry == null ||
                    DateTime.UtcNow > user.EmailConfirmationCodeExpiry ||
                    user.EmailConfirmationCode != confirmationDto.Code)
                {
                    return false;
                }

                return await _userRepository.ConfirmEmailAsync(user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while confirming email for: {Email}", confirmationDto.Email);
                return false;
            }
        }

        public async Task<bool> ResendConfirmationEmailAsync(string email)
        {
            try
            {
                var user = await FindUserByEmailAsync(email);
                if (user == null || user.EmailConfirmed)
                {
                    return true;
                }

                var code = GenerateOtpCode();
                user.EmailConfirmationCode = code;
                user.EmailConfirmationCodeExpiry = DateTime.UtcNow.AddMinutes(15);
                await _userRepository.UpdateAsync(user);

                await _mailService.SendEmailConfirmationAsync(user.Email, code, user.UserName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while resending confirmation email for: {Email}", email);
                return false;
            }
        }

        public async Task<bool> UpdateProfileAsync(int userId, UpdateProfileRequest dto)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return false;
                }

                if (!string.Equals(user.UserName, dto.UserName, StringComparison.OrdinalIgnoreCase) &&
                    await _userRepository.IsUsernameTakenAsync(dto.UserName))
                {
                    return false;
                }

                user.FirstName = dto.FirstName;
                user.LastName = dto.LastName;
                user.UserName = dto.UserName;
                user.NormalizedUserName = NormalizeName(dto.UserName);
                user.ConcurrencyStamp = Guid.NewGuid().ToString();
                return await _userRepository.UpdateAsync(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePassword dto)
        {
            try
            {
                if (dto.NewPassword != dto.ConfirmNewPassword)
                {
                    return false;
                }

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return false;
                }

                var passwordResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.CurrentPassword);
                if (passwordResult == PasswordVerificationResult.Failed)
                {
                    return false;
                }

                var passwordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
                return await _userRepository.UpdatePasswordHashAsync(user.Id, passwordHash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", userId);
                return false;
            }
        }

        private async Task<User?> FindUserByEmailAsync(string email)
        {
            var normalizedEmail = EmailNormalizer.NormalizeEmail(email);
            return await _userRepository.GetByNormalizedEmailAsync(normalizedEmail)
                ?? await _userRepository.GetByEmailAsync(email);
        }

        private async Task SetPasswordResetCodeAsync(User user)
        {
            user.PasswordResetCode = GenerateOtpCode();
            user.PasswordResetCodeExpiry = DateTime.UtcNow.AddMinutes(15);
            await _userRepository.UpdateAsync(user);
        }

        private async Task SendPasswordResetCodeAsync(User user)
        {
            try
            {
                await _mailService.SendPasswordResetCodeAsync(user.Email, user.PasswordResetCode!, user.UserName ?? user.Email);
                _logger.LogInformation("Password reset code sent successfully to user {Email}", user.Email);
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Failed to send password reset code to user {Email}", user.Email);
            }
        }

        private static bool IsLockedOut(User user)
        {
            return user.LockoutEnabled && user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow;
        }

        private static string NormalizeName(string value)
        {
            return value.Trim().ToUpperInvariant();
        }

        private static string GenerateOtpCode()
        {
            var digits = new char[6];
            var buffer = new byte[6];
            RandomNumberGenerator.Fill(buffer);
            for (var i = 0; i < 6; i++)
            {
                digits[i] = (char)('0' + buffer[i] % 10);
            }

            return new string(digits);
        }

        private static string GenerateResetToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }
    }
}
