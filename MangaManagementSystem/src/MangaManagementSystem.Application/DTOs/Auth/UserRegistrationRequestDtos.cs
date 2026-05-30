using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Auth
{
    public record UserRegistrationRequestDto(
        long RegistrationRequestId,
        int UserId,
        short RequestedRoleId,
        long? PortfolioFileId,
        string Status,
        int? ReviewedByUserId
    );

    public record CreateUserRegistrationRequestDto(
        [Required] int UserId,
        [Required] short RequestedRoleId,
        long? PortfolioFileId
    );

    public record UpdateUserRegistrationRequestStatusDto(
        [Required] long RegistrationRequestId,
        [Required][MaxLength(30)] string Status,
        int? ReviewedByUserId
    );
}
