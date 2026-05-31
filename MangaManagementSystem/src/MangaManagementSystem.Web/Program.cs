using MangaManagementSystem.Application;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Infrastructure;
using MangaManagementSystem.Web.Components;
using MangaManagementSystem.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Mvc;
using MudBlazor.Services;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddApplicationServices();
            builder.Services.AddInfrastructure(builder.Configuration);

            builder.Services.AddMemoryCache();
            builder.Services.AddSingleton<IOtpCacheService, OtpCacheService>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddAntiforgery();

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.LoginPath = "/login";
                options.AccessDeniedPath = "/pending-approval";
            })
            .AddGoogle(options =>
            {
                options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "YOUR_GOOGLE_CLIENT_ID";
                options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "YOUR_GOOGLE_CLIENT_SECRET";
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.Events.OnRemoteFailure = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError(context.Failure, "Google OAuth Remote Failure: {Message}", context.Failure?.Message);

                    context.HandleResponse();
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = 500;
                    var errorJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        Message = "Google OAuth Handshake Failed (OnRemoteFailure)",
                        ExceptionMessage = context.Failure?.Message,
                        StackTrace = context.Failure?.StackTrace
                    });

                    return context.Response.WriteAsync(errorJson);
                };
            });

            builder.Services.AddAuthorization();
            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
            builder.Services.AddScoped<CustomAuthenticationStateProvider>(sp =>
                (CustomAuthenticationStateProvider)sp.GetRequiredService<AuthenticationStateProvider>());

            builder.Services.AddMudServices();
            builder.Services.AddScoped<ToastService>();

            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseAntiforgery();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapPost("/api/auth/login", async (
                HttpContext context,
                IAuthService authService,
                [FromForm] string username,
                [FromForm] string password) =>
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    return Results.Redirect("/login?error=InvalidCredentials");
                }

                var result = await authService.LoginAsync(new LoginDto(username, password));
                if (!result.Succeeded || result.User is null || string.IsNullOrWhiteSpace(result.RoleName))
                {
                    return Results.Redirect("/login?error=InvalidCredentials");
                }

                await SignInApplicationUserAsync(context, result.User, result.RoleName);
                return Results.Redirect(GetDashboardRedirectUrl(result.User.RoleId));
            }).DisableAntiforgery();

            app.MapPost("/api/auth/google-login", () =>
            {
                var properties = new AuthenticationProperties { RedirectUri = "/api/auth/google-callback" };
                return Results.Challenge(properties, [GoogleDefaults.AuthenticationScheme]);
            }).DisableAntiforgery();

            app.MapGet("/api/auth/google-callback", async (HttpContext context, IAuthService authService) =>
            {
                var googleResult = await context.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
                var cookieResult = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                if (!googleResult.Succeeded && !cookieResult.Succeeded && context.User.Identity?.IsAuthenticated != true)
                {
                    return Results.Json(new
                    {
                        Message = "Authentication failed at callback. Diagnostic Info:",
                        GoogleSucceeded = googleResult.Succeeded,
                        GoogleFailureMessage = googleResult.Failure?.Message,
                        CookieSucceeded = cookieResult.Succeeded,
                        CookieFailureMessage = cookieResult.Failure?.Message,
                        IsAuthenticated = context.User.Identity?.IsAuthenticated,
                        RequestUrl = context.Request.Path + context.Request.QueryString,
                        QueryParams = context.Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString())
                    });
                }

                var principal = googleResult.Principal ?? cookieResult.Principal ?? context.User;

                var email = principal.FindFirst(ClaimTypes.Email)?.Value
                    ?? principal.FindFirst("email")?.Value
                    ?? principal.Claims.FirstOrDefault(c => c.Type.Contains("emailaddress", StringComparison.OrdinalIgnoreCase))?.Value;

                if (string.IsNullOrWhiteSpace(email))
                {
                    return Results.Redirect("/login?error=GoogleEmailMissing");
                }

                var authResult = await authService.GetUserByEmailAsync(email);
                if (!authResult.Succeeded || authResult.User is null || string.IsNullOrWhiteSpace(authResult.RoleName))
                {
                    return Results.Redirect("/login?error=UserNotInDatabase");
                }

                //await context.SignOutAsync(GoogleDefaults.AuthenticationScheme);
                await SignInApplicationUserAsync(context, authResult.User, authResult.RoleName);

                return Results.Redirect(GetDashboardRedirectUrl(authResult.User.RoleId));
            });

            app.MapPost("/api/auth/logout", async (HttpContext context, ILogger<Program> logger) =>
            {
                logger.LogInformation("Logout requested for user {Name}", context.User.Identity?.Name ?? "(anonymous)");
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.Redirect("/login");
            }).DisableAntiforgery();

            app.MapGet("/signout", async (CustomAuthenticationStateProvider authStateProvider) =>
            {
                await authStateProvider.MarkUserAsLoggedOut();
                return Results.Redirect("/login");
            });

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }

        private static async Task SignInApplicationUserAsync(HttpContext context, UserDto user, string roleName)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, roleName)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14)
                });
        }

        private static string GetDashboardRedirectUrl(short roleId) => roleId switch
        {
            1 => "/mangaka",
            2 => "/assistant",
            3 => "/dashboard",
            4 => "/ranking",
            5 => "/admin/user-approval",
            _ => "/login?error=InvalidCredentials"
        };
    }
}
