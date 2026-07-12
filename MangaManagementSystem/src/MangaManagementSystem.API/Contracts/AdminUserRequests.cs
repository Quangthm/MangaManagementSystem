using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.API.Contracts
{
    public sealed record AdminUserActionRequest(
        [MaxLength(500)]
        string? Reason = null);

    public sealed record AdminPasswordResetRequest(
        [Required]
        [MaxLength(2048)]
        string ResetPageUrl);
}
