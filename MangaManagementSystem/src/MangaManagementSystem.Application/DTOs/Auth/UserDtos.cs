using System;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Auth
{
    public record UserDto(
        int UserId,
        short RoleId,
        string Username,
        string Email,
        long? AvatarFileId,
        long? PortfolioFileId,
        string StatusCode,
        DateTime CreatedAtUtc
    );

    public record CreateUserDto(
        [Required] short RoleId,
        [Required][MaxLength(50)] string Username,
        [Required][MaxLength(254)] string Email,
        [Required][MaxLength(255)] string Password,
        long? AvatarFileId,
        long? PortfolioFileId
    );

    public record UpdateUserDto(
        [Required] int UserId,
        [Required] short RoleId,
        [Required][MaxLength(50)] string Username,
        [Required][MaxLength(254)] string Email,
        long? AvatarFileId,
        long? PortfolioFileId,
        [Required][MaxLength(30)] string StatusCode
    );
}
