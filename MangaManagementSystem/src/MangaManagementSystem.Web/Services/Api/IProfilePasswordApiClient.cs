namespace MangaManagementSystem.Web.Services.Api
{
    public interface IProfilePasswordApiClient
    {
        Task SendOtpAsync();

        Task ResetPasswordAsync(
            string otpCode,
            string newPassword);
    }
}
