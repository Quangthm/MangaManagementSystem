namespace MangaManagementSystem.Web.Services.Api
{
    public interface IProfilePasswordApiClient
    {
        Task SendOtpAsync(Guid userId);

        Task ResetPasswordAsync(
            Guid userId,
            string otpCode,
            string newPassword);
    }
}