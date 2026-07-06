using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.API.Contracts
{
    public sealed record RequestPasswordResetRequest(
        [Required]
        [EmailAddress]
        [MaxLength(254)]
        string Email,

        [Required]
        [Url]
        [MaxLength(1000)]
        string ResetPageUrl);

    public sealed record CompletePasswordResetRequest(
        [Required]
        [MaxLength(200)]
        string Token,

        [Required]
        [MinLength(8)]
        [MaxLength(255)]
        string NewPassword);
}
