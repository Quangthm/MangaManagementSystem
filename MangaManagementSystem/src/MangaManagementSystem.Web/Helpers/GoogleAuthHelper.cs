using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;

namespace MangaManagementSystem.Web.Helpers
{
    internal static class GoogleAuthHelper
    {
        internal static async Task<(ClaimsPrincipal Principal, string? Email, string? DisplayName)> ResolveGoogleIdentityAsync(
            HttpContext context)
        {
            var googleResult = await context.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
            var cookieResult = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            ClaimsPrincipal? principal = null;
            if (googleResult.Succeeded && googleResult.Principal is not null)
            {
                principal = googleResult.Principal;
            }
            else if (cookieResult.Succeeded && cookieResult.Principal is not null)
            {
                principal = cookieResult.Principal;
            }
            else if (context.User.Identity?.IsAuthenticated == true)
            {
                principal = context.User;
            }

            if (principal is null)
            {
                return (new ClaimsPrincipal(new ClaimsIdentity()), null, null);
            }

            var email = principal.FindFirst(ClaimTypes.Email)?.Value
                ?? principal.FindFirst("email")?.Value
                ?? principal.Claims.FirstOrDefault(c => c.Type.Contains("emailaddress", StringComparison.OrdinalIgnoreCase))?.Value;

            var displayName = principal.FindFirst(ClaimTypes.Name)?.Value
                ?? principal.FindFirst("name")?.Value;

            return (principal, email, displayName);
        }
    }
}
