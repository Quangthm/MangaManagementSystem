namespace MangaManagementSystem.Web.Services.Api
{
    public interface IPasswordResetApiClient
    {
        Task RequestPasswordResetAsync(
            string email,
            string resetPageUrl,
            CancellationToken cancellationToken = default);

        Task ResetPasswordAsync(
            string token,
            string newPassword,
            CancellationToken cancellationToken = default);
    }
}
