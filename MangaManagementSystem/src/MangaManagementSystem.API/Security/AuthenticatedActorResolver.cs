using System.Security.Claims;
using MangaManagementSystem.Application.Interfaces;

namespace MangaManagementSystem.API.Security;

public enum AuthenticatedActorFailureKind
{
    None,
    InvalidIdentity,
    UserNotFound,
    InactiveAccount,
    WrongRole
}

public sealed record AuthenticatedActorResult(
    bool Succeeded,
    Guid ActorUserId,
    AuthenticatedActorFailureKind FailureKind)
{
    public static AuthenticatedActorResult Success(Guid actorUserId) =>
        new(true, actorUserId, AuthenticatedActorFailureKind.None);

    public static AuthenticatedActorResult Failure(
        AuthenticatedActorFailureKind failureKind) =>
        new(false, Guid.Empty, failureKind);
}

public interface IAuthenticatedActorResolver
{
    Task<AuthenticatedActorResult> ResolveAsync(
        ClaimsPrincipal principal,
        string requiredRole);
}

public sealed class AuthenticatedActorResolver : IAuthenticatedActorResolver
{
    private const string ActiveStatusCode = "ACTIVE";

    private readonly IUserService _userService;

    public AuthenticatedActorResolver(IUserService userService)
    {
        _userService = userService;
    }

    public async Task<AuthenticatedActorResult> ResolveAsync(
        ClaimsPrincipal principal,
        string requiredRole)
    {
        var actorUserIdValue =
            principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value
            ?? principal.FindFirst("user_id")?.Value;

        if (!Guid.TryParse(actorUserIdValue, out var actorUserId)
            || actorUserId == Guid.Empty)
        {
            return AuthenticatedActorResult.Failure(
                AuthenticatedActorFailureKind.InvalidIdentity);
        }

        var actor = await _userService.GetUserByIdAsync(actorUserId);

        if (actor is null)
        {
            return AuthenticatedActorResult.Failure(
                AuthenticatedActorFailureKind.UserNotFound);
        }

        if (!string.Equals(
                actor.StatusCode,
                ActiveStatusCode,
                StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticatedActorResult.Failure(
                AuthenticatedActorFailureKind.InactiveAccount);
        }

        if (!string.Equals(
                actor.RoleName,
                requiredRole,
                StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticatedActorResult.Failure(
                AuthenticatedActorFailureKind.WrongRole);
        }

        return AuthenticatedActorResult.Success(actorUserId);
    }
}
