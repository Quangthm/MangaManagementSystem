using MailKit.Net.Smtp;
using MailKit.Security;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace MangaManagementSystem.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IOptions<SmtpSettings> settings,
            ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public Task SendOtpEmailAsync(
            string toEmail,
            string otpCode,
            CancellationToken cancellationToken = default)
        {
            var subject =
                "Your MangaFlow verification code";

            var body = string.Join(
                Environment.NewLine,
                "Hello,",
                string.Empty,
                $"Your MangaFlow verification code is: {otpCode}",
                string.Empty,
                "This code expires in 5 minutes.",
                string.Empty,
                "If you did not request this code, you can ignore this email.",
                string.Empty,
                "- MangaFlow");

            return SendEmailAsync(
                toEmail,
                subject,
                body,
                mockDetail: $"OTP: {otpCode}",
                cancellationToken);
        }

        public Task SendPasswordResetEmailAsync(
            string toEmail,
            string resetLink,
            CancellationToken cancellationToken = default)
        {
            var subject =
                "Reset your MangaFlow password";

            var body = string.Join(
                Environment.NewLine,
                "Hello,",
                string.Empty,
                "A password reset was requested for your MangaFlow account.",
                string.Empty,
                "Open this one-time link to choose a new password:",
                resetLink,
                string.Empty,
                "This link expires in 30 minutes and can be used only once.",
                string.Empty,
                "If you did not request this reset, you can ignore this email.",
                string.Empty,
                "- MangaFlow");

            return SendEmailAsync(
                toEmail,
                subject,
                body,
                mockDetail: $"Reset link: {resetLink}",
                cancellationToken);
        }

        public Task SendAccountApprovedEmailAsync(
            string toEmail,
            string displayName,
            CancellationToken cancellationToken = default)
        {
            var normalizedDisplayName =
                string.IsNullOrWhiteSpace(displayName)
                    ? "there"
                    : displayName.Trim();

            var subject =
                "Your MangaFlow account has been approved";

            var body = string.Join(
                Environment.NewLine,
                $"Hello {normalizedDisplayName},",
                string.Empty,
                "Your MangaFlow account has been approved.",
                string.Empty,
                "You can now sign in and start using your account.",
                string.Empty,
                "If you did not request this account, you can ignore this email.",
                string.Empty,
                "- MangaFlow");

            return SendEmailAsync(
                toEmail,
                subject,
                body,
                mockDetail: "Account approval notification",
                cancellationToken);
        }

        private async Task SendEmailAsync(
            string toEmail,
            string subject,
            string body,
            string mockDetail,
            CancellationToken cancellationToken)
        {
            if (_settings.UseMock)
            {
                _logger.LogInformation(
                    "Mock SMTP email to {Email}. Subject: {Subject}. {MockDetail}",
                    toEmail,
                    subject,
                    mockDetail);

                return;
            }

            var message = new MimeMessage();

            message.From.Add(
                new MailboxAddress(
                    _settings.FromName,
                    _settings.FromEmail));

            message.To.Add(
                MailboxAddress.Parse(toEmail));

            message.Subject = subject;
            message.Body =
                new TextPart("plain")
                {
                    Text = body
                };

            using var client = new SmtpClient();

            await client.ConnectAsync(
                _settings.Host,
                _settings.Port,
                _settings.UseSsl
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.Auto,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(
                    _settings.Username))
            {
                await client.AuthenticateAsync(
                    _settings.Username,
                    _settings.Password,
                    cancellationToken);
            }

            await client.SendAsync(
                message,
                cancellationToken);

            await client.DisconnectAsync(
                true,
                cancellationToken);

            _logger.LogInformation(
                "SMTP email sent to {Email}. Subject: {Subject}",
                toEmail,
                subject);
        }
    }
}
