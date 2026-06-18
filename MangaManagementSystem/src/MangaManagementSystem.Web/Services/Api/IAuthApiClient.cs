using MangaManagementSystem.Application.DTOs.Auth;

namespace MangaManagementSystem.Web.Services.Api
{
    public interface IAuthApiClient
    {
        Task<UserDto> LoginAsync(
            string usernameOrEmail,
            string password);

        Task<UserDto> ResolveGoogleLoginAsync(
            string email);

        Task<GoogleSignupCallbackResult>
            ProcessGoogleSignupAsync(
                string email,
                string? googleDisplayName,
                string roleName);
    }
}
