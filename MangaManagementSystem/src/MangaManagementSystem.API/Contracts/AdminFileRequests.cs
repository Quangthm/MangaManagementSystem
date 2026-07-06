using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.API.Contracts
{
    public sealed record AdminFileSoftDeleteRequest(
        [Required]
        [MaxLength(500)]
        string DeleteReason);
}
