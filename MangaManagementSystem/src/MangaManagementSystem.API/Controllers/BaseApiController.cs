using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace MangaManagementSystem.API.Controllers;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected Guid ResolveActorUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("userId")?.Value
            ?? User.FindFirst("UserId")?.Value
            ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

        if (Guid.TryParse(userIdClaim, out var userId))
            return userId;

        var headerClaim = Request.Headers["X-Actor-User-Id"].FirstOrDefault();
        if (Guid.TryParse(headerClaim, out var headerUserId))
            return headerUserId;

        throw new UnauthorizedAccessException("User ID could not be resolved. Ensure the user is authenticated and has a valid name identifier claim.");
    }
}
