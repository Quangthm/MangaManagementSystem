using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Auth
{
    public record RegisterDto(
        [Required][MaxLength(50)] string Username,
        [Required][MaxLength(254)] string Email,
        [Required][MinLength(8)][MaxLength(255)] string Password,
        [Required][MaxLength(30)] string RoleName,
        [MaxLength(100)] string? DisplayName,
        // Optional portfolio upload included in the cached registration. Bytes may be null when no file selected.
        string? PortfolioFileName = null,
        string? PortfolioContentType = null,
        byte[]? PortfolioFileBytes = null
    );

    public record LoginDto(
        [Required][MaxLength(254)] string UsernameOrEmail,
        [Required][MaxLength(255)] string Password
    );

    public record AuthResultDto(
        bool Succeeded,
        UserDto? User,
        string? RoleName,
        string? ErrorMessage
    );
}
