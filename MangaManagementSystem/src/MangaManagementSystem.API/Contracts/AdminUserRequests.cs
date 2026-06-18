using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.API.Contracts
{
    public sealed record AdminUserActionRequest(
        [MaxLength(500)]
        string? Reason = null);
}
