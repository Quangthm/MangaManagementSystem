namespace MangaManagementSystem.Application.Interfaces
{
    public sealed record PasswordResetTokenPayload(
        Guid UserId,
        string Email,
        DateTimeOffset ExpiresAtUtc);

    public interface IPasswordResetTokenService
    {
        string IssueToken(
            Guid userId,
            string email,
            TimeSpan lifetime);

        bool TryConsumeToken(
            string token,
            out PasswordResetTokenPayload payload);
    }
}
