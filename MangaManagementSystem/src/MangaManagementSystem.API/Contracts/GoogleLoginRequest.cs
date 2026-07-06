using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.API.Contracts
{
    public sealed record GoogleLoginRequest(
        [Required]
        [EmailAddress]
        [MaxLength(254)]
        string Email);
}
