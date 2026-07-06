using System.Security.Claims;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Web.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;

namespace MangaManagementSystem.Web.Services
{
    public class CustomAuthenticationStateProvider
        : AuthenticationStateProvider
    {
        private readonly IHttpContextAccessor
            _httpContextAccessor;
        private readonly AuthenticationSessionOptions
            _sessionOptions;

        public CustomAuthenticationStateProvider(
            IHttpContextAccessor httpContextAccessor,
            IOptions<AuthenticationSessionOptions> sessionOptions)
        {
            _httpContextAccessor =
                httpContextAccessor;
            _sessionOptions =
                sessionOptions.Value;
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
                        ResolveCookieExpiresAt()
                });

            NotifyAuthenticationStateChanged(
                Task.FromResult(
                    new AuthenticationState(
                        principal)));
        }

        private DateTimeOffset ResolveCookieExpiresAt()
        {
            if (_sessionOptions.CookieExpireMinutes <= 0)
            {
                throw new InvalidOperationException(
                    "AuthenticationSession:CookieExpireMinutes must be greater than zero.");
            }

            return DateTimeOffset.UtcNow.AddMinutes(
                _sessionOptions.CookieExpireMinutes);
        }    }
}
