using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.API.Contracts
{
    public record LoginRequest(
        [Required] string UsernameOrEmail,
        [Required] string Password
    );
}
