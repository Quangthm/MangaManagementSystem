using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.API.Contracts
{
    public sealed record GoogleSignupRequest(
        [Required]
        [EmailAddress]
        [MaxLength(254)]
        string Email,

        [MaxLength(100)]
        string? GoogleDisplayName,

        [Required]
        [MaxLength(30)]
        string RoleName);
}
