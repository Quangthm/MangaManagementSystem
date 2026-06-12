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

        public EmailService(IOptions<SmtpSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendOtpEmailAsync(string toEmail, string otpCode, CancellationToken cancellationToken = default)
        {
            var subject = "Your MangaFlow verification code";
            var body = $"""
                Hello,

                Your MangaFlow verification code is: {otpCode}

                This code expires in 5 minutes.

                If you did not request this code, you can ignore this email.

                — MangaFlow
                """;

            if (_settings.UseMock)
            {
                _logger.LogInformation(
                    "Mock SMTP email to {Email}. Subject: {Subject}. OTP: {Otp}",
                    toEmail,
                    subject,
                    otpCode);
                return;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();

            await client.ConnectAsync(
                _settings.Host,
                _settings.Port,
                _settings.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(_settings.Username))
            {
                await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("SMTP email sent to {Email}. Subject: {Subject}", toEmail, subject);
        }
    }
}