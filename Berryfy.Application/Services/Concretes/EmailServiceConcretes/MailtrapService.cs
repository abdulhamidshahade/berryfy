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

            var digits = confirmationCode.ToCharArray();

            var emailRequest = new SendEmailRequest
            {
                From = new EmailAddress("support@berryfy.org"),
                To = new List<EmailAddress> { new EmailAddress(email) },
                Subject = "Email Confirmation",
                HtmlBody = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Welcome to Berryfy - Confirm Your Email</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                        .content {{ background-color: #f8f9fa; padding: 30px; border-radius: 0 0 5px 5px; }}
                        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
                        .welcome {{ background-color: #d4edda; border: 1px solid #c3e6cb; padding: 15px; border-radius: 4px; margin: 15px 0; }}
                        .code-box {{ display: flex; justify-content: center; gap: 8px; margin: 24px 0; }}
                        .digit {{ display: inline-block; width: 48px; height: 56px; line-height: 56px; text-align: center;
                                  font-size: 28px; font-weight: bold; color: #007bff;
                                  border: 2px solid #007bff; border-radius: 8px;
                                  background: #ffffff; }}
                        .features {{ background-color: #e9ecef; padding: 15px; border-radius: 4px; margin: 15px 0; }}
                        .warning {{ background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 10px; border-radius: 4px; margin: 15px 0; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>🫐 Welcome to Berryfy!</h1>
                            <h2>Verify Your Email Address</h2>
                        </div>
                        <div class='content'>
                            <div class='welcome'>
                                <h3>🎉 Welcome {userName}!</h3>
                                <p>Thank you for joining Berryfy! Enter the 6-digit code below in the verification page to activate your account.</p>
                            </div>

                            <p style='text-align:center; font-size: 16px;'>Your verification code is:</p>

                            <div class='code-box'>
                                {string.Join("", digits.Select(d => $"<span class='digit'>{d}</span>"))}
                            </div>

                            <div class='warning'>
                                <strong>⚠️ Important:</strong>
                                <ul>
                                    <li>This code expires in <strong>15 minutes</strong>.</li>
                                    <li>Do not share this code with anyone.</li>
                                    <li>If you didn't create an account, you can safely ignore this email.</li>
                                </ul>
                            </div>

                            <div class='features'>
                                <h4>🛍️ What's waiting for you:</h4>
                                <ul>
                                    <li>Access to exclusive deals and discounts</li>
                                    <li>Track your orders and delivery status</li>
                                    <li>Create wishlists for your favourite items</li>
                                    <li>Priority customer support</li>
                                </ul>
                            </div>

                            <p>Welcome aboard!<br><strong>The Berryfy Team</strong></p>
                        </div>
                        <div class='footer'>
                            <p>This email was sent because you created an account at Berryfy.</p>
                            <p>&copy; 2026 Berryfy. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>"
            };

            await clinet.Email().Send(emailRequest);
        }
        public async Task SendPasswordResetCodeAsync(string email, string code, string userName)
        {
            using var mailtrapClientFactory = new MailtrapClientFactory(ApiToken);
            var clinet = mailtrapClientFactory.CreateClient();

            var digits = code.ToCharArray();

            var emailRequest = new SendEmailRequest
            {
                From = new EmailAddress("support@berryfy.org"),
                To = new List<EmailAddress> { new EmailAddress(email) },
                Subject = "Password Reset Code",
                HtmlBody = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Reset your Berryfy password</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                        .content {{ background-color: #f8f9fa; padding: 30px; border-radius: 0 0 5px 5px; }}
                        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
                        .code-box {{ display: flex; justify-content: center; gap: 8px; margin: 24px 0; }}
                        .digit {{ display: inline-block; width: 48px; height: 56px; line-height: 56px; text-align: center;
                                  font-size: 28px; font-weight: bold; color: #007bff;
                                  border: 2px solid #007bff; border-radius: 8px;
                                  background: #ffffff; }}
                        .warning {{ background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 10px; border-radius: 4px; margin: 15px 0; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>🫐 Berryfy</h1>
                            <h2>Password reset</h2>
                        </div>
                        <div class='content'>
                            <p>Hello {userName},</p>
                            <p>We received a request to reset the password for your Berryfy account. Enter this 6-digit code on the reset page:</p>

                            <div class='code-box'>
                                {string.Join("", digits.Select(d => $"<span class='digit'>{d}</span>"))}
                            </div>

                            <div class='warning'>
                                <strong>Security:</strong>
                                <ul>
                                    <li>This code expires in <strong>15 minutes</strong>.</li>
                                    <li>If you did not request a reset, you can ignore this email.</li>
                                    <li>Never share this code with anyone.</li>
                                </ul>
                            </div>

                            <p>Best regards,<br><strong>The Berryfy Team</strong></p>
                        </div>
                        <div class='footer'>
                            <p>&copy; 2026 Berryfy. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>"
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
