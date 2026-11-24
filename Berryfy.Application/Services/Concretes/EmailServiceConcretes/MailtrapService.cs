using Berryfy.Application.Dtos.EmailDtos.Requests;
using Berryfy.Application.Services.Interfaces.EmailServiceInterfaces;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Berryfy.Application.Services.Concretes.EmailServiceConcretes
{
    public class MailtrapService : IMailService
    {
        private static readonly HttpClient HttpClient = new();
        private const string MailtrapSendEndpoint = "https://send.api.mailtrap.io/api/send";
        private readonly string _apiToken;
        private readonly string _fromEmail;

        public MailtrapService(IConfiguration config)
        {
            _apiToken = config["Mailtrap:ApiToken"] ?? string.Empty;
            _fromEmail = config["Mailtrap:From"] ?? config["ResendSettings:From"] ?? "support@berryfy.org";
        }

        private async Task SendViaMailtrapAsync(string toEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(_apiToken))
            {
                throw new InvalidOperationException("Mailtrap:ApiToken is not configured.");
            }

            var payload = new
            {
                from = new { email = _fromEmail, name = "Berryfy" },
                to = new[] { new { email = toEmail } },
                subject,
                html = htmlBody
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, MailtrapSendEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            using var response = await HttpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Mailtrap API error ({(int)response.StatusCode}): {errorBody}");
            }
        }

        public async Task SendEmailAsync(SendEmailRequestDto request)
        {
            await SendViaMailtrapAsync(request.Recipient, request.Subject, request.Body);
        }

        public async Task SendEmailConfirmationAsync(string email, string confirmationCode, string userName)
        {
            var digits = confirmationCode.ToCharArray();

            var htmlBody = $@"
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

            await SendViaMailtrapAsync(email, "Email Confirmation", htmlBody);
        }

        public async Task SendPasswordResetCodeAsync(string email, string code, string userName)
        {
            var digits = code.ToCharArray();

            var htmlBody = $@"
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

            await SendViaMailtrapAsync(email, "Password Reset Code", htmlBody);
        }

        public async Task SendPasswordResetEmailAsync(string email, string resetToken, string resetUrl)
        {
            var htmlBody = $"<p>You requested a password reset. Click the link below to reset your password:</p><a href='{resetUrl}?token={resetToken}'>Reset Password</a>";
            await SendViaMailtrapAsync(email, "Password Reset Request", htmlBody);
        }
    }
}
