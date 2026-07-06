using MangaManagementSystem.Application.DTOs.Auth;

namespace MangaManagementSystem.API.Services
{
    public sealed record JwtTokenIssueResult(
        string AccessToken,
        DateTime ExpiresAtUtc);

    public interface IJwtTokenService
    {
        JwtTokenIssueResult IssueToken(
            UserDto user,
            string roleName);
    }
}