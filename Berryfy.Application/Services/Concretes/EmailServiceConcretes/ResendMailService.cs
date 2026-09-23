using Berryfy.Application.Config;
using Berryfy.Application.Dtos.EmailDtos;
using Berryfy.Application.Dtos.EmailDtos.Requests;
using Berryfy.Application.Services.Interfaces.EmailServiceInterfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;

namespace Berryfy.Application.Services.Concretes.EmailServiceConcretes
{
    public class ResendMailService : IMailService
    {
        private readonly IResend _resend;
        private readonly ResendSettings _settings;
        private readonly ILogger<ResendMailService> _logger;

        public ResendMailService(
            IResend resend,
            IOptions<ResendSettings> settings,
            ILogger<ResendMailService> logger)
        {
            _resend = resend;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(SendEmailRequest request)
        {
            try
            {
                var message = new EmailMessage
                {
                    From = _settings.From,
                    Subject = request.Subject,
                };
                message.To.Add(request.Recipient);

                if (request.IsBodyHtml)
                    message.HtmlBody = request.Body;
                else
                    message.TextBody = request.Body;

                await _resend.EmailSendAsync(message);

                _logger.LogInformation("Email sent successfully to {Recipient}", request.Recipient);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Recipient}", request.Recipient);
                throw;
            }
        }

        public async Task SendPasswordResetEmailAsync(string email, string resetToken, string resetUrl)
        {
            var encodedToken = Uri.EscapeDataString(resetToken);
            var encodedEmail = Uri.EscapeDataString(email);
            var resetLink = $"{resetUrl}?email={encodedEmail}&token={encodedToken}";

            var emailBody = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Password Reset - Berryfy</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                        .content {{ background-color: #f8f9fa; padding: 30px; border-radius: 0 0 5px 5px; }}
                        .button {{ display: inline-block; background-color: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
                        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
                        .warning {{ background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 10px; border-radius: 4px; margin: 15px 0; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>🫐 Berryfy</h1>
                            <h2>Password Reset Request</h2>
                        </div>
                        <div class='content'>
                            <p>Hello,</p>
                            <p>We received a request to reset your password for your Berryfy account. If you didn't make this request, you can safely ignore this email.</p>

                            <p>To reset your password, click the button below:</p>

                            <div style='text-align: center;'>
                                <a href='{resetLink}' class='button'>Reset My Password</a>
                            </div>

                            <p>Or copy and paste this link into your browser:</p>
                            <p style='word-break: break-all; background-color: #e9ecef; padding: 10px; border-radius: 4px;'>{resetLink}</p>

                            <div class='warning'>
                                <strong>⚠️ Security Notice:</strong>
                                <ul>
                                    <li>This link will expire in 24 hours for security reasons</li>
                                    <li>If you didn't request this reset, please contact our support team</li>
                                    <li>Never share this link with anyone</li>
                                </ul>
                            </div>

                            <p>If you're having trouble clicking the button, copy and paste the URL above into your web browser.</p>

                            <p>Best regards,<br>The Berryfy Team</p>
                        </div>
                        <div class='footer'>
                            <p>This email was sent from Berryfy. If you have any questions, please contact our support team.</p>
                            <p>&copy; 2024 Berryfy. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>";

            var sendRequest = new SendEmailRequest
            {
                Recipient = email,
                Subject = "Reset Your Berryfy account Password.",
                Body = emailBody,
                IsBodyHtml = true
            };

            await SendEmailAsync(sendRequest);
        }

        public async Task SendPasswordResetCodeAsync(string email, string code, string userName)
        {
            var digits = code.ToCharArray();

            var emailBody = $@"
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
                </html>";

            var sendRequest = new SendEmailRequest
            {
                Recipient = email,
                Subject = "Your Berryfy password reset code",
                Body = emailBody,
                IsBodyHtml = true
            };

            await SendEmailAsync(sendRequest);
        }

        public async Task SendEmailConfirmationAsync(string email, string confirmationCode, string userName)
        {
            var digits = confirmationCode.ToCharArray();

            var emailBody = $@"
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
                </html>";

            var sendRequest = new SendEmailRequest
            {
                Recipient = email,
                Subject = "Your Berryfy verification code",
                Body = emailBody,
                IsBodyHtml = true
            };

            await SendEmailAsync(sendRequest);
        }
    }
}
