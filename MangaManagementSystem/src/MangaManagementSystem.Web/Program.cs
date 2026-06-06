using MangaManagementSystem.Application;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Infrastructure;
using MangaManagementSystem.Web.Components;
using MangaManagementSystem.Web.Helpers;
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

                // Map common server-side messages to browser-friendly error codes so the UI
                // can show helpful but non-revealing messages.
                if (!result.Succeeded || result.User is null || string.IsNullOrWhiteSpace(result.RoleName))
                {
                    var error = (result.ErrorMessage ?? string.Empty).ToLowerInvariant();

                    if (error.Contains("pending"))
                    {
                        return Results.Redirect("/login?error=account_pending");
                    }

                    if (error.Contains("disabled"))
                    {
                        return Results.Redirect("/login?error=account_disabled");
                    }

                    if (error.Contains("reject") || error.Contains("rejected"))
                    {
                        return Results.Redirect("/login?error=account_rejected");
                    }

                    // Generic fallback for wrong username/password or other authentication failures.
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
                var (_, email, _) = await GoogleAuthHelper.ResolveGoogleIdentityAsync(context);

                if (string.IsNullOrWhiteSpace(email))
                {
                    return Results.Redirect("/login?error=GoogleEmailMissing");
                }

                var authResult = await authService.GetUserByEmailAsync(email);
                if (!authResult.Succeeded || authResult.User is null || string.IsNullOrWhiteSpace(authResult.RoleName))
                {
                    return Results.Redirect("/login?error=UserNotInDatabase");
                }

                await SignInApplicationUserAsync(context, authResult.User, authResult.RoleName);
                return Results.Redirect(GetDashboardRedirectUrl(authResult.User.RoleId));
            });

            app.MapPost("/api/auth/google-signup", () =>
            {
                var properties = new AuthenticationProperties { RedirectUri = "/api/auth/google-signup-callback" };
                return Results.Challenge(properties, [GoogleDefaults.AuthenticationScheme]);
            }).DisableAntiforgery();

            app.MapGet("/api/auth/google-signup-callback", async (HttpContext context, IAuthService authService) =>
            {
                var (_, email, displayName) = await GoogleAuthHelper.ResolveGoogleIdentityAsync(context);

                if (string.IsNullOrWhiteSpace(email))
                {
                    return Results.Redirect("/register?error=GoogleEmailMissing");
                }

                var signupResult = await authService.ProcessGoogleSignupCallbackAsync(email, displayName);

                return signupResult.Flow switch
                {
                    GoogleSignupFlow.NewUserVerifyOtp or GoogleSignupFlow.PendingApprovalVerifyOtp
                        => Results.Redirect($"/verify-otp?email={Uri.EscapeDataString(signupResult.Email)}"),
                    GoogleSignupFlow.ActiveUserLogin when signupResult.User is not null && signupResult.RoleName is not null
                        => await SignInAndRedirectAsync(context, signupResult.User, signupResult.RoleName),
                    GoogleSignupFlow.Rejected when signupResult.ErrorMessage?.Contains("pending", StringComparison.OrdinalIgnoreCase) == true
                        => Results.Redirect($"/login?error=account_pending"),
                    GoogleSignupFlow.Rejected
                        => Results.Redirect($"/register?error=account_disabled"),
                    _
                        => Results.Redirect("/register?error=GoogleSignupFailed")
                };
            });

            app.MapPost("/api/auth/verify-email-otp", async (
                HttpContext context,
                IAuthService authService,
                [FromForm] string email,
                [FromForm] string otp) =>
            {
                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otp))
                {
                    return Results.Redirect($"/verify-otp?email={Uri.EscapeDataString(email ?? string.Empty)}&error=InvalidOtp");
                }

                try
                {
                    await authService.CompleteEmailVerificationOtpAsync(email, otp);
                    return Results.Redirect("/login?verified=1");
                }
                catch (InvalidOperationException)
                {
                    return Results.Redirect($"/verify-otp?email={Uri.EscapeDataString(email)}&error=InvalidOtp");
                }
            }).DisableAntiforgery();

            app.MapPost("/api/auth/resend-email-otp", async (
                IAuthService authService,
                [FromForm] string email) =>
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return Results.Redirect("/register?error=EmailRequired");
                }

                try
                {
                    await authService.SendEmailVerificationOtpAsync(email);
                    return Results.Redirect($"/verify-otp?email={Uri.EscapeDataString(email)}&resent=1");
                }
                catch (InvalidOperationException ex)
                {
                    return Results.Redirect($"/verify-otp?email={Uri.EscapeDataString(email)}&error={Uri.EscapeDataString(ex.Message)}");
                }
            }).DisableAntiforgery();

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
            5 => "/board-decision",
            6 => "/admin/user-approval",
            _ => "/login?error=InvalidCredentials"
        };

        private static async Task<IResult> SignInAndRedirectAsync(
            HttpContext context,
            UserDto user,
            string roleName)
        {
            await SignInApplicationUserAsync(context, user, roleName);
            return Results.Redirect(GetDashboardRedirectUrl(user.RoleId));
        }
    }
}
