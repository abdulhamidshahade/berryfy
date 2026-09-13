using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.AuthDtos;
using Berryfy.Application.Halpers;
using Berryfy.Application.Services.Interfaces.AuthServiceInterfaces;
using Berryfy.Application.Services.Interfaces.EmailServiceInterfaces;
using Berryfy.Domain.Entities.AuthEntities;
using Berryfy.Domain.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Berryfy.Application.Services.Concretes.AuthServiceConcretes
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AuthService> _logger;
        private readonly ITokenService _tokenService;
        private readonly IRoleManagementService _roleService;
        private readonly IMailService _mailService;
        private readonly IUserService _userService;

        public AuthService(UserManager<ApplicationUser> userManager,
                           ILogger<AuthService> logger,
                           ITokenService tokenService,
                           IRoleManagementService roleService,
                           IMailService mailService,
                           IUserService userService)
        {
            _userManager = userManager;
            _logger = logger;
            _tokenService = tokenService;
            _roleService = roleService;
            _mailService = mailService;
            _userService = userService;
        }
        private static string GenerateOtpCode()
        {
            var digits = new char[6];
            var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var buffer = new byte[6];
            rng.GetBytes(buffer);
            for (int i = 0; i < 6; i++)
                digits[i] = (char)('0' + buffer[i] % 10);
            return new string(digits);
        }

        private async Task<bool> IsUsernameTaken(string userName)
        {
            return await _userService.IsUsernameTaken(userName);
        }
        private async Task<bool> IsEmailTaken(string email)
        {
            return await _userService.IsUserExistsByEmailAsync(email);
        }

        public async Task<Result<RegisterResponseDto>> Register(RegisterRequestDto requestDto)
        {
            if (requestDto == null)
            {
                return Result<RegisterResponseDto>.ValidationError("Registration data is required.");
            }

            if (await IsUsernameTaken(requestDto.UserName))
            {
                return Result<RegisterResponseDto>.ValidationError("Username is taken.");
            }

            if (await IsEmailTaken(requestDto.Email))
            {
                return Result<RegisterResponseDto>.ValidationError("Email is already registered.");
            }

            try
            {
                ApplicationUser user = new()
                {
                    FirstName = requestDto.FirstName,
                    LastName = requestDto.LastName,
                    UserName = requestDto.UserName,
                    Email = requestDto.Email,
                    NormalizedEmail = EmailNormalizer.NormalizeEmail(requestDto.Email)
                };

                var result = await _userManager.CreateAsync(user, requestDto.Password);

                if (result.Succeeded)
                {
                    var defaultRole = RoleConstants.User;

                    var isRoleAssignmentSuccess = await _roleService.AssignRoleToUserAsync(user.Id, defaultRole);

                    if (!isRoleAssignmentSuccess)
                    {
                        return Result<RegisterResponseDto>.Failure("User created but failed to assign default role.");
                    }

                    var userRoles = await _userManager.GetRolesAsync(user);

                    try
                    {
                        var code = GenerateOtpCode();
                        user.EmailConfirmationCode = code;
                        user.EmailConfirmationCodeExpiry = DateTime.UtcNow.AddMinutes(15);
                        await _userManager.UpdateAsync(user);

                        await _mailService.SendEmailConfirmationAsync(user.Email, code, user.UserName);
                        _logger.LogInformation("Email confirmation code sent successfully to {Email}", user.Email);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Failed to send email confirmation to user {Email}", user.Email);
                    }

                    ApplicationUserDto userDto = new()
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        UserName = user.UserName,
                        Roles = userRoles.ToList()
                    };

                    return Result<RegisterResponseDto>.Success(new RegisterResponseDto
                    {
                        IsSuccess = true,
                        User = userDto
                    });
                }

                return Result<RegisterResponseDto>.ValidationError(string.Join(", ", result.Errors.Select(e => e.Description)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while registering the user.");

                return Result<RegisterResponseDto>.Failure("An unexpected error occurred during registration. Please try again later.");
            }
        }

        public async Task<LoginResponseDto> Login(LoginRequestDto requestDto)
        {
            var normalizedEmail = requestDto.Email.Trim().ToLower();
            var user = await _userManager.FindByEmailAsync(normalizedEmail);

            if (user == null)
            {
                _logger.LogWarning("User not found for email: {Email}", normalizedEmail);
                return new LoginResponseDto { User = null, Token = "" };
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                return new LoginResponseDto { Token = string.Empty, ErrorMessage = "Account is locked. Please try again later." };
            }

            var isPasswordCorrect = await _userManager.CheckPasswordAsync(user, requestDto.Password);

            if (!isPasswordCorrect)
            {
                await _userManager.AccessFailedAsync(user);
                _logger.LogWarning("Invalid password for email: {Email}", normalizedEmail);
                return new LoginResponseDto { User = null, Token = "" };
            }

            if (!user.EmailConfirmed)
            {
                _logger.LogWarning("Login attempted with unconfirmed email: {Email}", normalizedEmail);
                return new LoginResponseDto { User = null, Token = "", ErrorMessage = "Email not confirmed. Please check your email and confirm your account." };
            }

            if (!(await _userManager.ResetAccessFailedCountAsync(user)).Succeeded)
            {
                return new LoginResponseDto { Token = string.Empty, ErrorMessage = "Unable to sign in. Please try again." };
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            ApplicationUserDto userDto = new()
            {
                Email = requestDto.Email,
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName,
                Roles = userRoles.ToList()
            };

            var token = await _tokenService.GenerateToken(user);
            var refreshToken = await _tokenService.GenerateRefreshToken(user);

            //TODO: add here login email alerting

            return new LoginResponseDto()
            {
                User = userDto,
                Token = token,
                RefreshToken = refreshToken
            };
        }

        public async Task<LoginResponseDto> RefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return new LoginResponseDto { Token = string.Empty, ErrorMessage = "Invalid or expired refresh token." };
            }
            var hashedToken = TokenService.HashToken(refreshToken);
            var user = _userManager.Users.FirstOrDefault(u => u.RefreshToken == hashedToken);

            if (user == null || user.RefreshTokenExpiry == null || user.RefreshTokenExpiry <= DateTime.UtcNow ||
                !user.EmailConfirmed || await _userManager.IsLockedOutAsync(user))
            {
                return new LoginResponseDto { Token = string.Empty, ErrorMessage = "Invalid or expired refresh token." };
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            ApplicationUserDto userDto = new()
            {
                Email = user.Email,
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserName = user.UserName,
                Roles = userRoles.ToList()
            };

            var newToken = await _tokenService.GenerateToken(user);
            var newRefreshToken = await _tokenService.GenerateRefreshToken(user);

            return new LoginResponseDto()
            {
                User = userDto,
                Token = newToken,
                RefreshToken = newRefreshToken
            };
        }

        public async Task<bool> ForgotPasswordAsync(ForgotPasswordRequestDto requestDto)
        {
            try
            {
                var normalizedEmail = requestDto.Email.Trim().ToLower();
                var user = await _userManager.FindByEmailAsync(normalizedEmail);

                if (user == null)
                {
                    _logger.LogWarning("Password reset requested for non-existent email: {Email}", normalizedEmail);
                    return true;
                }

                var code = GenerateOtpCode();
                user.PasswordResetCode = code;
                user.PasswordResetCodeExpiry = DateTime.UtcNow.AddMinutes(15);
                await _userManager.UpdateAsync(user);

                try
                {
                    await _mailService.SendPasswordResetCodeAsync(user.Email, code, user.UserName ?? user.Email);
                    _logger.LogInformation("Password reset code sent successfully to user {Email}", user.Email);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send password reset code to user {Email}", user.Email);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing forgot password request for email: {Email}",
                    requestDto.Email);
                return false;
            }
        }

        public async Task<VerifyPasswordResetCodeResponseDto?> VerifyPasswordResetCodeAsync(EmailConfirmationDto confirmationDto)
        {
            try
            {
                var normalizedEmail = confirmationDto.Email.Trim().ToLower();
                var user = await _userManager.FindByEmailAsync(normalizedEmail);

                if (user == null)
                {
                    _logger.LogWarning("Password reset code verification attempted for non-existent email: {Email}", normalizedEmail);
                    return null;
                }

                if (user.PasswordResetCode == null || user.PasswordResetCodeExpiry == null)
                {
                    _logger.LogWarning("No password reset code found for user {Email}", normalizedEmail);
                    return null;
                }

                if (DateTime.UtcNow > user.PasswordResetCodeExpiry)
                {
                    _logger.LogWarning("Password reset code expired for user {Email}", normalizedEmail);
                    return null;
                }

                if (user.PasswordResetCode != confirmationDto.Code)
                {
                    _logger.LogWarning("Invalid password reset code for user {Email}", normalizedEmail);
                    return null;
                }

                user.PasswordResetCode = null;
                user.PasswordResetCodeExpiry = null;
                await _userManager.UpdateAsync(user);

                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                _logger.LogInformation("Password reset code verified for user {Email}", user.Email);

                return new VerifyPasswordResetCodeResponseDto { ResetToken = resetToken };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while verifying password reset code for: {Email}",
                    confirmationDto.Email);
                return null;
            }
        }

        public async Task<bool> ResendPasswordResetCodeAsync(string email)
        {
            try
            {
                var normalizedEmail = email.Trim().ToLower();
                var user = await _userManager.FindByEmailAsync(normalizedEmail);

                if (user == null)
                {
                    _logger.LogWarning("Resend password reset requested for non-existent email: {Email}", normalizedEmail);
                    return true;
                }

                var code = GenerateOtpCode();
                user.PasswordResetCode = code;
                user.PasswordResetCodeExpiry = DateTime.UtcNow.AddMinutes(15);
                await _userManager.UpdateAsync(user);

                try
                {
                    await _mailService.SendPasswordResetCodeAsync(user.Email, code, user.UserName ?? user.Email);
                    _logger.LogInformation("Password reset code resent successfully to user {Email}", user.Email);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to resend password reset code to user {Email}", user.Email);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while resending password reset code for: {Email}", email);
                return false;
            }
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordRequestDto requestDto)
        {
            try
            {
                var normalizedEmail = requestDto.Email.Trim().ToLower();
                var user = await _userManager.FindByEmailAsync(normalizedEmail);

                if (user == null)
                {
                    _logger.LogWarning("Password reset attempted for non-existent email: {Email}", normalizedEmail);
                    return false;
                }

                var result = await PasswordSessionRevocation.ExecuteAsync(user,
                    () => _userManager.ResetPasswordAsync(user, requestDto.Token, requestDto.NewPassword));

                if (result.Succeeded)
                {
                    _logger.LogInformation("Password reset successful for user {Email}", user.Email);
                    return true;
                }

                _logger.LogWarning("Password reset failed for user {Email}. Errors: {Errors}",
                    user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while resetting password for email: {Email}",
                    requestDto.Email);
                return false;
            }
        }

        public async Task<bool> ConfirmEmailAsync(EmailConfirmationDto confirmationDto)
        {
            try
            {
                var normalizedEmail = confirmationDto.Email.Trim().ToLower();
                var user = await _userManager.FindByEmailAsync(normalizedEmail);

                if (user == null)
                {
                    _logger.LogWarning("Email confirmation attempted for non-existent email: {Email}", normalizedEmail);
                    return false;
                }

                if (user.EmailConfirmed)
                {
                    _logger.LogInformation("Email already confirmed for user {Email}", user.Email);
                    return true;
                }

                if (user.EmailConfirmationCode == null || user.EmailConfirmationCodeExpiry == null)
                {
                    _logger.LogWarning("No confirmation code found for user {Email}", normalizedEmail);
                    return false;
                }

                if (DateTime.UtcNow > user.EmailConfirmationCodeExpiry)
                {
                    _logger.LogWarning("Confirmation code expired for user {Email}", normalizedEmail);
                    return false;
                }

                if (user.EmailConfirmationCode != confirmationDto.Code)
                {
                    _logger.LogWarning("Invalid confirmation code for user {Email}", normalizedEmail);
                    return false;
                }

                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var result = await _userManager.ConfirmEmailAsync(user, token);

                if (result.Succeeded)
                {
                    user.EmailConfirmationCode = null;
                    user.EmailConfirmationCodeExpiry = null;
                    await _userManager.UpdateAsync(user);

                    _logger.LogInformation("Email confirmed successfully for user {Email}", user.Email);
                    return true;
                }

                _logger.LogWarning("Email confirmation failed for user {Email}. Errors: {Errors}",
                    user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while confirming email for: {Email}",
                    confirmationDto.Email);
                return false;
            }
        }

        public async Task<bool> ResendConfirmationEmailAsync(string email)
        {
            try
            {
                var normalizedEmail = email.Trim().ToLower();
                var user = await _userManager.FindByEmailAsync(normalizedEmail);

                if (user == null)
                {
                    _logger.LogWarning("Resend confirmation requested for non-existent email: {Email}", normalizedEmail);
                    return true;
                }

                if (user.EmailConfirmed)
                {
                    _logger.LogInformation("Resend confirmation requested for already confirmed email: {Email}", user.Email);
                    return true;
                }

                var code = GenerateOtpCode();
                user.EmailConfirmationCode = code;
                user.EmailConfirmationCodeExpiry = DateTime.UtcNow.AddMinutes(15);
                await _userManager.UpdateAsync(user);

                try
                {
                    await _mailService.SendEmailConfirmationAsync(user.Email, code, user.UserName);
                    _logger.LogInformation("Confirmation code resent successfully to user {Email}", user.Email);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to resend confirmation email to user {Email}", user.Email);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while resending confirmation email for: {Email}", email);
                return false;
            }
        }

        public async Task<bool> UpdateProfileAsync(int userId, UpdateProfileDto dto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null) return false;

                user.FirstName = dto.FirstName;
                user.LastName = dto.LastName;

                if (user.UserName != dto.UserName)
                {
                    var userNameTaken = await IsUsernameTaken(dto.UserName);
                    if (userNameTaken) return false;
                    user.UserName = dto.UserName;
                }

                var result = await _userManager.UpdateAsync(user);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto)
        {
            try
            {
                if (dto.NewPassword != dto.ConfirmNewPassword) return false;

                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null) return false;

                var result = await PasswordSessionRevocation.ExecuteAsync(user,
                    () => _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword));
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", userId);
                return false;
            }
        }
    }
}
