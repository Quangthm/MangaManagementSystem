using MangaManagementSystem.Application.DTOs.Auth;
using MediatR;

namespace MangaManagementSystem.Application.Features.Auth.Registration
{
    public sealed record ProcessGoogleSignupCommand(
        string Email,
        string? GoogleDisplayName,
        string RoleName
    ) : IRequest<GoogleSignupCallbackResult>;
}
