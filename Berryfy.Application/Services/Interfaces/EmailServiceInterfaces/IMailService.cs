using Berryfy.Application.Dtos.EmailDtos.Requests;

namespace Berryfy.Application.Services.Interfaces.EmailServiceInterfaces
{
    public interface IMailService
    {
        Task SendEmailAsync(SendEmailRequest request);
        Task SendPasswordResetEmailAsync(string email, string resetToken, string resetUrl);
        Task SendPasswordResetCodeAsync(string email, string code, string userName);
        Task SendEmailConfirmationAsync(string email, string confirmationCode, string userName);
    }
}
