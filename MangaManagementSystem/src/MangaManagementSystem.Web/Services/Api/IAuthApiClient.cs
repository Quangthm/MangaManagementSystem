using MangaManagementSystem.Application.DTOs.Auth;

namespace MangaManagementSystem.Web.Services.Api;

public interface IAuthApiClient
{
    Task<LoginApiResult> LoginAsync(
        string usernameOrEmail,
        string password,
        CancellationToken cancellationToken = default);
}

public sealed record LoginApiResult(
    UserDto User,
    string RoleName,
    string AccessToken,
    DateTime ExpiresAtUtc);