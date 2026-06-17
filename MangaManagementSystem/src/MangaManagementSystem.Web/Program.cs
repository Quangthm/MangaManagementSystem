using MangaManagementSystem.Application;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Infrastructure;
using MangaManagementSystem.Web.Components;
using MangaManagementSystem.Web.Helpers;
using MangaManagementSystem.Web.Services;
using MangaManagementSystem.Web.Services.Api;
using MangaManagementSystem.Web.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Mvc;
using MudBlazor.Services;
using System.Security.Claims;

namespace MangaManagementSystem.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddApplicationServices();
            builder.Services.AddInfrastructure(builder.Configuration);

            builder.Services.Configure<RecaptchaOptions>(builder.Configuration.GetSection(RecaptchaOptions.SectionName));
            builder.Services.AddHttpClient<RecaptchaService>();

            builder.Services.AddMemoryCache();
            builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection(ApiSettings.SectionName));

            builder.Services
                .AddOptions<InternalApiOptions>()
                .Bind(
                    builder.Configuration.GetSection(
                        InternalApiOptions.SectionName))
                .Validate(
                    options =>
                        !string.IsNullOrWhiteSpace(
                            options.Key),
                    "InternalApi:Key is required.")
                .ValidateOnStart();
            builder.Services.AddHttpClient<IRegistrationApiClient, RegistrationApiClient>((sp, client) =>
            {
                var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>();
                client.BaseAddress = new Uri(settings.Value.BaseUrl);
            });
            builder.Services.AddHttpClient<IAuthApiClient, AuthApiClient>((sp, client) =>
            {
                var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>();
                client.BaseAddress = new Uri(settings.Value.BaseUrl);
            });
            builder.Services.AddHttpClient<IMangakaSeriesApiClient, MangakaSeriesApiClient>((sp, client) =>
            {
                var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>();
                client.BaseAddress = new Uri(settings.Value.BaseUrl);
            });
            builder.Services.AddHttpClient<IProfilePasswordApiClient, ProfilePasswordApiClient>((sp, client) =>
            {
                var settings =
                    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>();

                client.BaseAddress =
                    new Uri(settings.Value.BaseUrl);
            });
            builder.Services.AddHttpClient<IProfileApiClient, ProfileApiClient>((sp, client) =>
            {
                var settings =
                    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>();

                client.BaseAddress =
                    new Uri(settings.Value.BaseUrl);
            });
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddAntiforgery();

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultScheme =
                    CookieAuthenticationDefaults.AuthenticationScheme;

                options.DefaultAuthenticateScheme =
                    CookieAuthenticationDefaults.AuthenticationScheme;

                options.DefaultSignInScheme =
                    CookieAuthenticationDefaults.AuthenticationScheme;

                options.DefaultChallengeScheme =
                    CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.LoginPath = "/login";
                options.AccessDeniedPath = "/access-denied";
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
                .AddInteractiveServerComponents()
                .AddHubOptions(options =>
                {
                    options.MaximumReceiveMessageSize = 50 * 1024 * 1024; // 50MB
                });

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
                IAuthApiClient authApi,
                RecaptchaService recaptchaService,
                [FromForm] string username,
                [FromForm] string password) =>
            {
                var recaptchaResponse = context.Request.Form["g-recaptcha-response"].ToString();
                var ip = context.Connection.RemoteIpAddress?.ToString();
                var isRecaptchaValid = await recaptchaService.VerifyTokenAsync(recaptchaResponse, ip);
                if (!isRecaptchaValid)
                {
                    return Results.Redirect("/login?error=RecaptchaFailed");
                }

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    return Results.Redirect("/login?error=InvalidCredentials");
                }

                try
                {
                    var user = await authApi.LoginAsync(username, password);

                    if (string.IsNullOrWhiteSpace(user.RoleName))
                    {
                        return Results.Redirect("/login?error=InvalidCredentials");
                    }

                    await SignInApplicationUserAsync(context, user, user.RoleName);
                    return Results.Redirect(GetDashboardRedirectUrl(user.RoleName));
                }
                catch (InvalidOperationException ex)
                {
                    var error = (ex.Message ?? string.Empty).ToLowerInvariant();

                    if (error.Contains("pending"))
                    {
                        return Results.Redirect(
                            "/pending-approval");
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
            }).DisableAntiforgery();

            app.MapPost("/api/auth/google-login", () =>
            {
                var properties =
                    new AuthenticationProperties
                    {
                        RedirectUri =
                            "/api/auth/google-callback"
                    };

                return Results.Challenge(
                    properties,
                    [GoogleDefaults.AuthenticationScheme]);
            }).DisableAntiforgery();

            app.MapGet(
                "/api/auth/google-callback",
                async (
                    HttpContext context,
                    IAuthService authService) =>
                {
                    var (_, email, _, _) =
                        await GoogleAuthHelper
                            .ResolveGoogleIdentityAsync(context);

                    if (string.IsNullOrWhiteSpace(email))
                    {
                        await context.SignOutAsync(
                            CookieAuthenticationDefaults
                                .AuthenticationScheme);

                        return Results.Redirect(
                            "/login?error=GoogleEmailMissing");
                    }

                    var authResult =
                        await authService.GetUserByEmailAsync(email);

                    if (!authResult.Succeeded
    || authResult.User is null
    || string.IsNullOrWhiteSpace(
        authResult.RoleName))
                    {
                        await context.SignOutAsync(
                            CookieAuthenticationDefaults
                                .AuthenticationScheme);

                        var error =
                            (authResult.ErrorMessage
                                ?? string.Empty)
                            .ToLowerInvariant();

                        if (error.Contains("pending"))
                        {
                            return Results.Redirect(
                                "/pending-approval");
                        }

                        if (error.Contains("disabled"))
                        {
                            return Results.Redirect(
                                "/login?error=account_disabled");
                        }

                        if (error.Contains("reject"))
                        {
                            return Results.Redirect(
                                "/login?error=account_rejected");
                        }

                        return Results.Redirect(
                            "/login?error=UserNotInDatabase");
                    }

                    await SignInApplicationUserAsync(
                        context,
                        authResult.User,
                        authResult.RoleName);

                    return Results.Redirect(
                        GetDashboardRedirectUrl(
                            authResult.RoleName));
                });

            app.MapPost(
                "/api/auth/google-signup",
                ([FromForm] string roleName) =>
                {
                    var properties =
                        new AuthenticationProperties
                        {
                            RedirectUri =
                                "/api/auth/google-signup-callback"
                        };

                    properties.Items[
                        GoogleAuthHelper.RegistrationRoleProperty] =
                            roleName;

                    return Results.Challenge(
                        properties,
                        [GoogleDefaults.AuthenticationScheme]);
                })
                .DisableAntiforgery();

            app.MapGet(
                "/api/auth/google-signup-callback",
                async (
                    HttpContext context,
                    IAuthApiClient authApi,
                    ILogger<Program> logger) =>
                {
                    var (
                        _,
                        email,
                        displayName,
                        registrationRole) =
                            await GoogleAuthHelper
                                .ResolveGoogleIdentityAsync(context);

                    if (string.IsNullOrWhiteSpace(email))
                    {
                        await context.SignOutAsync(
                            CookieAuthenticationDefaults
                                .AuthenticationScheme);

                        return Results.Redirect(
                            "/register?error=GoogleEmailMissing");
                    }

                    if (!MangaManagementSystem
                            .Application
                            .Features
                            .Auth
                            .Registration
                            .PublicRegistrationRoles
                            .TryNormalize(
                                registrationRole,
                                out var normalizedRoleName))
                    {
                        await context.SignOutAsync(
                            CookieAuthenticationDefaults
                                .AuthenticationScheme);

                        return Results.Redirect(
                            "/register?error=InvalidRole");
                    }

                    try
                    {
                        var signupResult =
                            await authApi.ProcessGoogleSignupAsync(
                                email,
                                displayName,
                                normalizedRoleName);

                        if (signupResult.Flow
                                == GoogleSignupFlow.ActiveUserLogin
                            && signupResult.User is not null
                            && !string.IsNullOrWhiteSpace(
                                signupResult.RoleName))
                        {
                            return await SignInAndRedirectAsync(
                                context,
                                signupResult.User,
                                signupResult.RoleName);
                        }

                        await context.SignOutAsync(
                            CookieAuthenticationDefaults
                                .AuthenticationScheme);

                        return signupResult.Flow switch
                        {
                            GoogleSignupFlow.PendingApproval =>
                                Results.Redirect(
                                    "/pending-approval"),

                            GoogleSignupFlow.Rejected =>
                                Results.Redirect(
                                    "/login?error=account_rejected"),

                            GoogleSignupFlow.Disabled =>
                                Results.Redirect(
                                    "/login?error=account_disabled"),

                            _ =>
                                Results.Redirect(
                                    "/register?error=GoogleSignupFailed")
                        };
                    }
                    catch (InvalidOperationException ex)
                    {
                        logger.LogWarning(
                            ex,
                            "Google sign-up API request was rejected.");

                        await context.SignOutAsync(
                            CookieAuthenticationDefaults
                                .AuthenticationScheme);

                        return Results.Redirect(
                            "/register?error=GoogleSignupFailed");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(
                            ex,
                            "Unexpected Google sign-up callback failure.");

                        await context.SignOutAsync(
                            CookieAuthenticationDefaults
                                .AuthenticationScheme);

                        return Results.Redirect(
                            "/register?error=GoogleSignupFailed");
                    }
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

app.MapGet("/signout", async (CustomAuthenticationStateProvider authStateProvider, HttpContext context) =>
{
    await authStateProvider.MarkUserAsLoggedOut();
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});



            app.MapGet(
    "/api/portfolio/{id:guid}",
    async (
        Guid id,
        HttpContext context,
        IUserService userService,
        IFileResourceService fileResourceService,
        ILogger<Program> logger) =>
    {
        if (id == Guid.Empty)
        {
            return Results.BadRequest(
                new
                {
                    message =
                        "Portfolio file id is required."
                });
        }

        if (context.User.Identity?.IsAuthenticated
            != true)
        {
            return Results.Json(
                new
                {
                    message =
                        "Authentication is required."
                },
                statusCode:
                    StatusCodes.Status401Unauthorized);
        }

        var actorUserIdValue =
            context.User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(
                actorUserIdValue,
                out var actorUserId)
            || actorUserId == Guid.Empty)
        {
            return Results.Json(
                new
                {
                    message =
                        "Authenticated user information is invalid."
                },
                statusCode:
                    StatusCodes.Status401Unauthorized);
        }

        var actor =
            await userService.GetUserByIdAsync(
                actorUserId);

        if (actor is null)
        {
            return Results.Json(
                new
                {
                    message =
                        "Authenticated user was not found."
                },
                statusCode:
                    StatusCodes.Status401Unauthorized);
        }

        if (!string.Equals(
                actor.StatusCode,
                "ACTIVE",
                StringComparison.OrdinalIgnoreCase))
        {
            return Results.Json(
                new
                {
                    message =
                        "The account is not active."
                },
                statusCode:
                    StatusCodes.Status403Forbidden);
        }

        var portfolioOwner =
            await userService
                .GetUserByPortfolioFileIdAsync(id);

        if (portfolioOwner is null)
        {
            return Results.NotFound(
                new
                {
                    message =
                        "Portfolio file was not found."
                });
        }

        var actorIsOwner =
            actor.UserId ==
            portfolioOwner.UserId;

        var actorIsAdmin =
            string.Equals(
                actor.RoleName,
                "Admin",
                StringComparison.OrdinalIgnoreCase);

        if (!actorIsOwner && !actorIsAdmin)
        {
            logger.LogWarning(
                "User {ActorUserId} attempted to access portfolio {PortfolioFileId} owned by user {OwnerUserId}.",
                actor.UserId,
                id,
                portfolioOwner.UserId);

            return Results.Json(
                new
                {
                    message =
                        "You do not have permission to access this portfolio."
                },
                statusCode:
                    StatusCodes.Status403Forbidden);
        }

        try
        {
            var file =
                await fileResourceService
                    .GetFileResourceByIdAsync(id);

            if (file is null
                || file.DeletedAtUtc is not null
                || string.IsNullOrWhiteSpace(
                    file.CloudinarySecureUrl))
            {
                return Results.NotFound(
                    new
                    {
                        message =
                            "Portfolio file was not found."
                    });
            }

            logger.LogInformation(
                "User {ActorUserId} is loading portfolio {PortfolioFileId}.",
                actor.UserId,
                id);

            using var httpClient =
                new HttpClient();

            httpClient.DefaultRequestHeaders.Add(
                "User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

            httpClient.DefaultRequestHeaders.Add(
                "Accept",
                "*/*");

            using var response =
                await httpClient.GetAsync(
                    file.CloudinarySecureUrl);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError(
                    "Portfolio {PortfolioFileId} download failed with HTTP {StatusCode}.",
                    id,
                    (int)response.StatusCode);

                return Results.Json(
                    new
                    {
                        message =
                            "The portfolio file could not be loaded."
                    },
                    statusCode:
                        StatusCodes
                            .Status502BadGateway);
            }

            var bytes =
                await response.Content
                    .ReadAsByteArrayAsync();

            var contentType =
                string.IsNullOrWhiteSpace(
                    file.ContentType)
                    ? "application/octet-stream"
                    : file.ContentType;

            return Results.Bytes(
                bytes,
                contentType);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Unexpected error loading portfolio {PortfolioFileId} for user {ActorUserId}.",
                id,
                actor.UserId);

            return Results.Json(
                new
                {
                    message =
                        "The portfolio file could not be loaded."
                },
                statusCode:
                    StatusCodes
                        .Status500InternalServerError);
        }
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

        private static string GetDashboardRedirectUrl(
            string roleName) =>
            roleName switch
    {
        "Admin" =>
            "/admin",

        "Mangaka" =>
            "/mangaka",

        "Assistant" =>
            "/assistant",

        "Tantou Editor" =>
            "/editor",

        "Editorial Board Member" =>
            "/board",

        "Editorial Board Chief" =>
            "/board-chief",

        _ =>
            "/access-denied"
    };

        private static async Task<IResult> SignInAndRedirectAsync(
            HttpContext context,
            UserDto user,
            string roleName)
        {
            await SignInApplicationUserAsync(context, user, roleName);
            return Results.Redirect(GetDashboardRedirectUrl(roleName));
        }
    }
}
