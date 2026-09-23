using Berryfy.Application.Dtos;
using Berryfy.Application.Dtos.AuthDtos;
using Berryfy.Application.Dtos.AuthDtos.Requests;
using Berryfy.Application.Dtos.AuthDtos.Responses;

namespace Berryfy.Application.Services.Interfaces.AuthServiceInterfaces
{
    public interface IAuthService
    {
        Task<ApplicationResponse<RegisterResponse>> Register(RegisterRequest requestDto);
        Task<LoginResponse> Login(LoginRequest requestDto);
        Task<LoginResponse> RefreshTokenAsync(string refreshToken);
        Task<bool> ForgotPasswordAsync(ForgotPasswordRequest requestDto);
        Task<VerifyPasswordResetCodeResponse?> VerifyPasswordResetCodeAsync(EmailConfirmation confirmationDto);
        Task<bool> ResendPasswordResetCodeAsync(string email);
        Task<bool> ResetPasswordAsync(ResetPasswordRequest requestDto);
        Task<bool> ConfirmEmailAsync(EmailConfirmation confirmationDto);
        Task<bool> ResendConfirmationEmailAsync(string email);
        Task<bool> UpdateProfileAsync(int userId, UpdateProfileRequest dto);
        Task<bool> ChangePasswordAsync(int userId, ChangePassword dto);
    }
}
