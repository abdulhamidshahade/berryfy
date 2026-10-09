using Berryfy.Application.Dtos.EmailDtos.Requests;
using Berryfy.Application.Services.Interfaces.EmailServiceInterfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mailtrap;
using Mailtrap.Emails.Models;
using Mailtrap.Emails.Requests;

namespace Berryfy.Application.Services.Concretes.EmailServiceConcretes
{
    public class MailtrapService : IMailService
    {
        private readonly string ApiToken;

        public MailtrapService(IConfiguration config)
        {
            ApiToken = config.GetSection("Mailtrap:ApiToken").Value;
        }
        public async Task SendEmailAsync(SendEmailRequestDto request)
        {
            using var mailtrapClientFactory = new MailtrapClientFactory(ApiToken);
            var clinet = mailtrapClientFactory.CreateClient();

            var emailRequest = new SendEmailRequest
            {
                From = new EmailAddress("support@berryfy.org"),
                To = new List<EmailAddress> { new EmailAddress(request.Recipient) },
                Subject = request.Subject,
                HtmlBody = request.Body,
            };

            await clinet.Email().Send(emailRequest);
        }

        public async Task SendEmailConfirmationAsync(string email, string confirmationCode, string userName)
        {
            using var mailtrapClientFactory = new MailtrapClientFactory(ApiToken);
            var clinet = mailtrapClientFactory.CreateClient();

            var emailRequest = new SendEmailRequest
            {
                From = new EmailAddress("support@berryfy.org"),
                To = new List<EmailAddress> { new EmailAddress(email) },
                Subject = "Email Confirmation",
                HtmlBody = $"<p>Hi {userName},</p><p>Please confirm your email by clicking the link below:</p><a href='https://yourdomain.com/confirm-email?code={confirmationCode}'>Confirm Email</a>"
            };

            await clinet.Email().Send(emailRequest);
        }
        public async Task SendPasswordResetCodeAsync(string email, string code, string userName)
        {
            using var mailtrapClientFactory = new MailtrapClientFactory(ApiToken);
            var clinet = mailtrapClientFactory.CreateClient();

            var emailRequest = new SendEmailRequest
            {
                From = new EmailAddress("support@berryfy.org"),
                To = new List<EmailAddress> { new EmailAddress(email) },
                Subject = "Password Reset Code",
                HtmlBody = $"<p>Hi {userName},</p><p>Your password reset code is: <strong>{code}</strong></p>"
            };

            await clinet.Email().Send(emailRequest);
        }

        public async Task SendPasswordResetEmailAsync(string email, string resetToken, string resetUrl)
        {
            var mailtrapClientFactory = new MailtrapClientFactory(ApiToken);
            var client = mailtrapClientFactory.CreateClient();

            var emailRequest = new SendEmailRequest
            {
                From = new EmailAddress("support@berryfy.org"),
                To = new List<EmailAddress> { new EmailAddress(email) },
                Subject = "Password Reset Request",
                HtmlBody = $"<p>You requested a password reset. Click the link below to reset your password:</p><a href='{resetUrl}?token={resetToken}'>Reset Password</a>"
            };

            await client.Email().Send(emailRequest);
        }
    }
}
