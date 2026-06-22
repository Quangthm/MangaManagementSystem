using System;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Auth
{
    public record UserDto(
        Guid UserId,
        string Username,
        string DisplayName,
        string Email,
        Guid? AvatarFileId,
        Guid? PortfolioFileId,
        string StatusCode,
        DateTime CreatedAtUtc,
        string? RoleName
    );

    public record CreateUserDto(
        [Required][MaxLength(30)] string RoleName,
        [Required][MaxLength(50)] string Username,
        [MaxLength(100)] string? DisplayName,
        [Required][MaxLength(254)] string Email,
        [Required][MaxLength(255)] string Password,
        Guid? AvatarFileId,
        Guid? PortfolioFileId
    );

    public record UpdateUserDto(
        [Required] Guid UserId,
        [Required][MaxLength(30)] string RoleName,
        [Required][MaxLength(50)] string Username,
        [MaxLength(100)] string? DisplayName,
        [Required][MaxLength(254)] string Email,
        Guid? AvatarFileId,
        Guid? PortfolioFileId,
        [Required][MaxLength(30)] string StatusCode
    );
}
