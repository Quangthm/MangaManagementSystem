namespace MangaManagementSystem.Application.Interfaces;

public interface IEmailService
{
    Task SendOtpEmailAsync(string toEmail, string otpCode, CancellationToken cancellationToken = default);    public interface IEmailService
    {
        Task SendOtpEmailAsync(
            string toEmail,
            string otpCode,
            CancellationToken cancellationToken = default);

        Task SendPasswordResetEmailAsync(
            string toEmail,
            string resetLink,
            CancellationToken cancellationToken = default);

        Task SendAccountApprovedEmailAsync(
            string toEmail,
            string displayName,
            CancellationToken cancellationToken = default);
    }

}
