using MangaManagementSystem.Application.DTOs.Auth;
using MediatR;

namespace MangaManagementSystem.Application.Features.Auth.Queries
{
    public sealed record AuthenticateUserQuery(
        string UsernameOrEmail,
        string Password)
        : IRequest<AuthResultDto>;

    public sealed record ResolveGoogleLoginQuery(
        string Email)
        : IRequest<AuthResultDto>;
}
