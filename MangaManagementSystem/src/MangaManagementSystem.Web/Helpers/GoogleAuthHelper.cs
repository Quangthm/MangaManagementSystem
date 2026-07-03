using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;

namespace MangaManagementSystem.Web.Helpers
{
    internal static class GoogleAuthHelper
    {
        internal const string RegistrationRoleProperty =
            "registration_role";

        internal static async Task<(
            ClaimsPrincipal Principal,
            string? Email,
            string? DisplayName,
            string? RegistrationRole)>
            ResolveGoogleIdentityAsync(
                HttpContext context)
        {
            var googleResult =
                await context.AuthenticateAsync(
                    GoogleDefaults.AuthenticationScheme);

            var cookieResult =
                await context.AuthenticateAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);

            ClaimsPrincipal? principal = null;
            AuthenticationProperties? properties = null;

            if (googleResult.Succeeded
                && googleResult.Principal is not null)
            {
                principal = googleResult.Principal;
                properties = googleResult.Properties;
            }
            else if (cookieResult.Succeeded
                     && cookieResult.Principal is not null)
            {
                principal = cookieResult.Principal;
                properties = cookieResult.Properties;
            }
            else if (context.User.Identity?.IsAuthenticated == true)
            {
                principal = context.User;
            }

            if (principal is null)
            {
                return (
                    new ClaimsPrincipal(
                        new ClaimsIdentity()),
                    null,
                    null,
                    null);
            }

            var email =
                principal.FindFirst(
                    ClaimTypes.Email)?.Value
                ?? principal.FindFirst("email")?.Value
                ?? principal.Claims
                    .FirstOrDefault(
                        claim =>
                            claim.Type.Contains(
                                "emailaddress",
                                StringComparison.OrdinalIgnoreCase))
                    ?.Value;

            var displayName =
                principal.FindFirst(
                    ClaimTypes.Name)?.Value
                ?? principal.FindFirst("name")?.Value;

            string? registrationRole = null;

            if (properties?.Items.TryGetValue(
                    RegistrationRoleProperty,
                    out var storedRole) == true)
            {
                registrationRole = storedRole;
            }

            return (
                principal,
                email,
                displayName,
                registrationRole);
        }
    }
}
