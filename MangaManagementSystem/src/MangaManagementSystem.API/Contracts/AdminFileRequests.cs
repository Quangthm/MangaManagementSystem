using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.API.Contracts
{
    public sealed record AdminFileSoftDeleteRequest(
        [Required]
        [MaxLength(500)]
        string DeleteReason);

    public sealed record AdminFileCleanupRequest(
        [MaxLength(500)]
        string? Reason);
}
