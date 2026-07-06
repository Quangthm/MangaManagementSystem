using System.Security.Claims;
using MangaManagementSystem.Application.DTOs.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;

namespace MangaManagementSystem.Web.Services
{
    public class CustomAuthenticationStateProvider
        : AuthenticationStateProvider
    {
        private readonly IHttpContextAccessor
            _httpContextAccessor;

        public CustomAuthenticationStateProvider(
            IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor =
                httpContextAccessor;
        }

        public override Task<AuthenticationState>
            GetAuthenticationStateAsync()
        {
            var principal =
                _httpContextAccessor
                    .HttpContext
                    ?.User
                ?? new ClaimsPrincipal(
                    new ClaimsIdentity());

            return Task.FromResult(
                new AuthenticationState(principal));
        }

        public async Task MarkUserAsAuthenticated(
            AuthResultDto authResult)
        {
            if (authResult.User is null
                || string.IsNullOrWhiteSpace(
                    authResult.RoleName))
            {
                throw new InvalidOperationException(
                    "Authentication result must include user and role information.");
            }

            var claims =
                new List<Claim>
                {
                    new(
                        ClaimTypes.NameIdentifier,
                        authResult.User.UserId.ToString()),

                    new(
                        ClaimTypes.Name,
                        authResult.User.Username),

                    new(
                        ClaimTypes.Email,
                        authResult.User.Email),

                    new(
                        ClaimTypes.Role,
                        authResult.RoleName)
                };

            var identity =
                new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults
                        .AuthenticationScheme);

            var principal =
                new ClaimsPrincipal(identity);

            var httpContext =
                _httpContextAccessor.HttpContext
                ?? throw new InvalidOperationException(
                    "HttpContext is not available.");

            await httpContext.SignInAsync(
                CookieAuthenticationDefaults
                    .AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc =
                        DateTimeOffset.UtcNow
                            .AddDays(14)
                });

            NotifyAuthenticationStateChanged(
                Task.FromResult(
                    new AuthenticationState(
                        principal)));
        }
    }
}
