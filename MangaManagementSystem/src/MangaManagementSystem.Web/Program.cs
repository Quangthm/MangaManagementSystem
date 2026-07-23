using MangaManagementSystem.Application;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Infrastructure;
using MangaManagementSystem.Web.Components;
using MangaManagementSystem.Web.Helpers;
using MangaManagementSystem.Web.Services;
using MangaManagementSystem.Web.Services.Api;
using MangaManagementSystem.Web.Services.EditorialBoard;
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
            builder.Services.AddScoped<ApiAuthorizationMessageHandler>();
builder.Services.AddHttpClient<IRegistrationApiClient, RegistrationApiClient>((sp, client) =>
            {
                var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>();
                client.BaseAddress = new Uri(settings.Value.BaseUrl);
            });
            builder.Services
                .AddHttpClient<IEditorialBoardApiClient, EditorialBoardApiClient>((sp, client) =>
                {
                    var settings =
                        sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>();

                    client.BaseAddress =
                        new Uri(settings.Value.BaseUrl);
                })
                .AddHttpMessageHandler<ApiAuthorizationMessageHandler>();
            builder.Services
    .AddHttpClient<ISeriesRankingApiClient, SeriesRankingApiClient>((sp, client) =>
    {
        var settings =
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>();

        client.BaseAddress =
            new Uri(settings.Value.BaseUrl);
    })
    .AddHttpMessageHandler<ApiAuthorizationMessageHandler>();
            builder.Services.AddHttpClient<IAuthApiClient, AuthApiClient>((sp, client) =>
            {
                var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>();
                client.BaseAddress = new Uri(settings.Value.BaseUrl);
            });
            builder.Services.AddHttpClient<IPasswordResetApiClient, PasswordResetApiClient>((sp, client) =>
            {
                var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>();
                client.BaseAddress = new Uri(settings.Value.BaseUrl);
            });
            builder.Services
                .AddHttpClient<IMangakaSeriesApiClient, MangakaSeriesApiClient>((sp, client) =>
                {
                    var settings =
                        sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>();

                    client.BaseAddress =
                        new Uri(settings.Value.BaseUrl);
                })
                .AddHttpMessageHandler<ApiAuthorizationMessageHandler>();
            builder.Services.AddHttpClient<IMangakaSeriesContributorApiClient, MangakaSeriesContributorApiClient>((sp, client) =>
            {
                var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>();
                client.BaseAddress = new Uri(settings.Value.BaseUrl);
            })
                .AddHttpMessageHandler<ApiAuthorizationMessageHandler>();
            builder.Services.AddHttpClient<IProfilePasswordApiClient, ProfilePasswordApiClient>((sp, client) =>
            {
                var settings =
                    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>();

                client.BaseAddress =
                    new Uri(settings.Value.BaseUrl);
            });
            builder.Services
                .AddHttpClient<IProfileApiClient, ProfileApiClient>((sp, client) =>
                {
                    var settings =
                        sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>();

                    client.BaseAddress =
                        new Uri(settings.Value.BaseUrl);
                })
                .AddHttpMessageHandler<ApiAuthorizationMessageHandler>();
            builder.Services.AddHttpClient<IAdminUserApiClient, AdminUserApiClient>((sp, client) =>
            {
                var settings =
                    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>();

                client.BaseAddress =
                    new Uri(settings.Value.BaseUrl);
            })
                .AddHttpMessageHandler<ApiAuthorizationMessageHandler>();
            builder.Services.AddHttpClient<IAdminAuditApiClient, AdminAuditApiClient>((sp, client) =>
            {
                var settings =
                    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>();

                client.BaseAddress =
                    new Uri(settings.Value.BaseUrl);
            })
                .AddHttpMessageHandler<ApiAuthorizationMessageHandler>();

            builder.Services
                .AddHttpClient<IReferenceDataApiClient, ReferenceDataApiClient>((sp, client) =>
                {
                    var settings =
                        sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>();

                    client.BaseAddress =
                        new Uri(settings.Value.BaseUrl);
                })
                .AddHttpMessageHandler<ApiAuthorizationMessageHandler>();
builder.Services.AddHttpClient<IAdminFileApiClient, AdminFileApiClient>((sp, client) =>
            {
                var settings =
                    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>();

                client.BaseAddress =
                    new Uri(settings.Value.BaseUrl);
            })
                .AddHttpMessageHandler<ApiAuthorizationMessageHandler>();
            builder.Services
                .AddHttpClient<
                    INotificationApiClient,
                    NotificationApiClient>(
                    (serviceProvider, client) =>
                    {
                        var settings =
                            serviceProvider
                                .GetRequiredService<
                                    Microsoft.Extensions.Options
                                        .IOptions<ApiSettings>>();

                        client.BaseAddress =
                            new Uri(
                                settings.Value.BaseUrl);
                    })
                .AddHttpMessageHandler<
                    ApiAuthorizationMessageHandler>();

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
                    var logger =
                        context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<Program>>();

                    logger.LogError(
                        context.Failure,
                        "Google OAuth remote authentication failed.");

                    context.HandleResponse();

                    context.Response.Redirect(
                        $"/login?error={AuthErrorCodes.GoogleOAuthFailed}");

                    return Task.CompletedTask;
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

            // Register typed API clients
            builder.Services.AddHttpClient<Services.Api.IAssistantTaskApiClient, Services.Api.AssistantTaskApiClient>((sp, client) =>
            {
                var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>();
                client.BaseAddress = new Uri(settings.Value.BaseUrl);
            });
            builder.Services
                .AddHttpClient<Services.Api.ISeriesApiClient, Services.Api.SeriesApiClient>((sp, client) =>
                {
                    var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>();
                    client.BaseAddress = new Uri(settings.Value.BaseUrl);
                })
                .AddHttpMessageHandler<ApiAuthorizationMessageHandler>();
            builder.Services.AddHttpClient<Services.Api.IEditorProposalApiClient, Services.Api.EditorProposalApiClient>((sp, client) =>
            {
                var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>();
                client.BaseAddress = new Uri(settings.Value.BaseUrl);
            });
            builder.Services.AddHttpClient<Services.Api.IMangakaTaskApiClient, Services.Api.MangakaTaskApiClient>((sp, client) =>
            {
                var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>();
                client.BaseAddress = new Uri(settings.Value.BaseUrl);
            })
                .AddHttpMessageHandler<ApiAuthorizationMessageHandler>();
            builder.Services.AddHttpClient<Services.Api.IEditorDashboardApiClient, Services.Api.EditorDashboardApiClient>((sp, client) =>
            {
                var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>();
                client.BaseAddress = new Uri(settings.Value.BaseUrl);
            });
            builder.Services.AddHttpClient<Services.Api.IEditorChapterReviewApiClient, Services.Api.EditorChapterReviewApiClient>((sp, client) =>
            {
                var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>();
                client.BaseAddress = new Uri(settings.Value.BaseUrl);
            });
            builder.Services.AddHttpClient<Services.Api.IEditorAnnotationApiClient, Services.Api.EditorAnnotationApiClient>((sp, client) =>
            {
                var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>();
                client.BaseAddress = new Uri(settings.Value.BaseUrl);
            });
            builder.Services.AddHttpClient<Services.Api.IEditorSeriesApiClient, Services.Api.EditorSeriesApiClient>((sp, client) =>
            {
                var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>();
                client.BaseAddress = new Uri(settings.Value.BaseUrl);
            });
            builder.Services.AddHttpClient<Services.Api.IMangakaChapterApiClient, Services.Api.MangakaChapterApiClient>((sp, client) =>
            {
                var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>();
                client.BaseAddress = new Uri(settings.Value.BaseUrl);
            })
                .AddHttpMessageHandler<ApiAuthorizationMessageHandler>();
            builder.Services.AddHttpClient<Services.Api.IMangakaPageApiClient, Services.Api.MangakaPageApiClient>((sp, client) =>
            {
                var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>();
                client.BaseAddress = new Uri(settings.Value.BaseUrl);
            })
                .AddHttpMessageHandler<ApiAuthorizationMessageHandler>();
            builder.Services.AddHttpClient<Services.Api.IMangakaPageRegionApiClient, Services.Api.MangakaPageRegionApiClient>((sp, client) =>
            {
                var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>();
                client.BaseAddress = new Uri(settings.Value.BaseUrl);
            })
                .AddHttpMessageHandler<ApiAuthorizationMessageHandler>();
            builder.Services.AddHttpClient<Services.Api.IMangakaAnnotationApiClient, Services.Api.MangakaAnnotationApiClient>((sp, client) =>
            {
                var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>();
                client.BaseAddress = new Uri(settings.Value.BaseUrl);
            })
                .AddHttpMessageHandler<ApiAuthorizationMessageHandler>();

            builder.Services.AddHttpClient<Services.Api.IPublicationScheduleApiClient, Services.Api.PublicationScheduleApiClient>((sp, client) =>
            {
                var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ApiSettings>>();
                client.BaseAddress = new Uri(settings.Value.BaseUrl);
            });

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();  
            app.UseStaticFiles();

            app.UseAuthentication();
            app.UseAuthorization();
            app.UseAntiforgery();

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
                    return Results.Redirect($"/login?error={AuthErrorCodes.RecaptchaFailed}");
                }

                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    return Results.Redirect($"/login?error={AuthErrorCodes.InvalidCredentials}");
                }

                try
                {
                    var loginResult =
                        await authApi.LoginAsync(
                            username,
                            password);

                    if (string.IsNullOrWhiteSpace(
                            loginResult.RoleName))
                    {
                        return Results.Redirect(
                            $"/login?error={AuthErrorCodes.InvalidCredentials}");
                    }

                    await SignInApplicationUserAsync(
                        context,
                        loginResult.User,
                        loginResult.RoleName,
                        loginResult.AccessToken,
                        loginResult.ExpiresAtUtc);

                    return Results.Redirect(
                        GetDashboardRedirectUrl(
                            loginResult.RoleName));
                }
                catch (ApiClientException ex)
                {
                    if (ex.Code ==
                        AuthErrorCodes.AccountPending)
                    {
                        return Results.Redirect(
                            "/pending-approval");
                    }

                    return Results.Redirect(
                        BuildLoginErrorRedirect(
                            ex.Code));
                }
                catch (Exception)
                {
                    return Results.Redirect(
                        BuildLoginErrorRedirect(
                            AuthErrorCodes.InvalidCredentials));
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
                    IAuthApiClient authApi,
                    ILogger<Program> logger) =>
                {
                    var (_, email, _, _) =
                        await GoogleAuthHelper
                            .ResolveGoogleIdentityAsync(
                                context);

                    if (string.IsNullOrWhiteSpace(email))
                    {
                        await context.SignOutAsync(
                            CookieAuthenticationDefaults
                                .AuthenticationScheme);

                        return Results.Redirect(
                            BuildLoginErrorRedirect(
                                AuthErrorCodes
                                    .GoogleEmailMissing));
                    }

                    try
                    {
                        var loginResult =
                            await authApi
                                .ResolveGoogleLoginAsync(
                                    email);

                        if (string.IsNullOrWhiteSpace(
                                loginResult.RoleName)
                            || string.IsNullOrWhiteSpace(
                                loginResult.AccessToken))
                        {
                            throw new ApiClientException(
                                AuthErrorCodes
                                    .AccountConfigurationInvalid,
                                "Account configuration is invalid.",
                                System.Net.HttpStatusCode
                                    .Forbidden);
                        }

                        return await SignInAndRedirectAsync(
                            context,
                            loginResult);
                    }
                    catch (ApiClientException ex)
                    {
                        logger.LogWarning(
                            "Google login was rejected with code {ErrorCode}.",
                            ex.Code);

                        await context.SignOutAsync(
                            CookieAuthenticationDefaults
                                .AuthenticationScheme);

                        if (ex.Code ==
                            AuthErrorCodes.AccountPending)
                        {
                            return Results.Redirect(
                                "/pending-approval");
                        }

                        return Results.Redirect(
                            BuildLoginErrorRedirect(
                                ex.Code));
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(
                            ex,
                            "Unexpected Google login callback failure.");

                        await context.SignOutAsync(
                            CookieAuthenticationDefaults
                                .AuthenticationScheme);

                        return Results.Redirect(
                            BuildLoginErrorRedirect(
                                AuthErrorCodes
                                    .GoogleOAuthFailed));
                    }
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
                            BuildRegisterErrorRedirect(
                                AuthErrorCodes
                                    .GoogleEmailMissing));
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
                            BuildRegisterErrorRedirect(
                                AuthErrorCodes.InvalidRole));
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
                            var loginResult =
                                await authApi
                                    .ResolveGoogleLoginAsync(
                                        email);

                            return await SignInAndRedirectAsync(
                                context,
                                loginResult);
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
                                    BuildLoginErrorRedirect(
                                        AuthErrorCodes
                                            .AccountRejected)),

                            GoogleSignupFlow.Disabled =>
                                Results.Redirect(
                                    BuildLoginErrorRedirect(
                                        AuthErrorCodes
                                            .AccountDisabled)),

                            _ =>
                                Results.Redirect(
                                    BuildRegisterErrorRedirect(
                                        signupResult.ErrorCode
                                        ?? AuthErrorCodes
                                            .GoogleSignupFailed))
                        };
                    }
                    catch (ApiClientException ex)
                    {
                        logger.LogWarning(
                            "Google sign-up API request was rejected with code {ErrorCode}.",
                            ex.Code);

                        await context.SignOutAsync(
                            CookieAuthenticationDefaults
                                .AuthenticationScheme);

                        return Results.Redirect(
                            BuildRegisterErrorRedirect(
                                ex.Code));
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
                            BuildRegisterErrorRedirect(
                                AuthErrorCodes
                                    .GoogleSignupFailed));
                    }
                });

app.MapPost(
    "/api/auth/logout",
    async (
        HttpContext context,
        Microsoft.AspNetCore.Antiforgery
            .IAntiforgery antiforgery,
        ILogger<Program> logger) =>
    {
        try
        {
            await antiforgery
                .ValidateRequestAsync(context);
        }
        catch (
            Microsoft.AspNetCore.Antiforgery
                .AntiforgeryValidationException)
        {
            logger.LogWarning(
                "Rejected logout request because the antiforgery token was invalid.");

            return Results.BadRequest(
                new
                {
                    message =
                        "Invalid logout request."
                });
        }

        var username =
            context.User.Identity?.Name
            ?? "(anonymous)";

        await context.SignOutAsync(
            CookieAuthenticationDefaults
                .AuthenticationScheme);

        logger.LogInformation(
            "User {Name} logged out.",
            username);

        return Results.Redirect("/login");
    })
    .RequireAuthorization();




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

        private static async Task
            SignInApplicationUserAsync(
                HttpContext context,
                UserDto user,
                string roleName,
                string? accessToken = null,
                DateTime? expiresAtUtc = null)
        {
            var claims =
                new List<Claim>
                {
                    new(
                        ClaimTypes.NameIdentifier,
                        user.UserId.ToString()),
                    new(
                        ClaimTypes.Name,
                        user.Username),
                    new(
                        ClaimTypes.Email,
                        user.Email),
                    new(
                        ClaimTypes.Role,
                        roleName)
                };

            if (!string.IsNullOrWhiteSpace(
                    accessToken))
            {
                claims.Add(
                    new Claim(
                        "api_access_token",
                        accessToken));
            }

            var identity =
                new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults
                        .AuthenticationScheme);

            var principal =
                new ClaimsPrincipal(identity);

            var cookieExpiresAt =
                expiresAtUtc.HasValue
                    ? new DateTimeOffset(
                        DateTime.SpecifyKind(
                            expiresAtUtc.Value,
                            DateTimeKind.Utc))
                    : DateTimeOffset.UtcNow
                        .AddDays(14);

            await context.SignInAsync(
                CookieAuthenticationDefaults
                    .AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = cookieExpiresAt
                });
        }

        private static string BuildLoginErrorRedirect(
            string? errorCode)
        {
            var normalizedCode =
                errorCode switch
                {
                    AuthErrorCodes.AccountPending =>
                        AuthErrorCodes.AccountPending,

                    AuthErrorCodes.AccountRejected =>
                        AuthErrorCodes.AccountRejected,

                    AuthErrorCodes.AccountDisabled =>
                        AuthErrorCodes.AccountDisabled,

                    AuthErrorCodes.AccountNotFound =>
                        AuthErrorCodes.AccountNotFound,

                    AuthErrorCodes.AccountConfigurationInvalid =>
                        AuthErrorCodes.AccountConfigurationInvalid,

                    AuthErrorCodes.GoogleEmailMissing =>
                        AuthErrorCodes.GoogleEmailMissing,

                    AuthErrorCodes.GoogleOAuthFailed =>
                        AuthErrorCodes.GoogleOAuthFailed,

                    AuthErrorCodes.RecaptchaFailed =>
                        AuthErrorCodes.RecaptchaFailed,

                    _ =>
                        AuthErrorCodes.InvalidCredentials
                };

            return "/login?error="
                + Uri.EscapeDataString(
                    normalizedCode);
        }

        private static string BuildRegisterErrorRedirect(
            string? errorCode)
        {
            var normalizedCode =
                errorCode switch
                {
                    AuthErrorCodes.GoogleEmailMissing =>
                        AuthErrorCodes.GoogleEmailMissing,

                    AuthErrorCodes.InvalidRole =>
                        AuthErrorCodes.InvalidRole,

                    AuthErrorCodes.AccountPending =>
                        AuthErrorCodes.AccountPending,

                    AuthErrorCodes.AccountRejected =>
                        AuthErrorCodes.AccountRejected,

                    AuthErrorCodes.AccountDisabled =>
                        AuthErrorCodes.AccountDisabled,

                    AuthErrorCodes.EmailRequired =>
                        AuthErrorCodes.EmailRequired,

                    AuthErrorCodes.EmailAlreadyExists =>
                        AuthErrorCodes.EmailAlreadyExists,

                    AuthErrorCodes.UsernameTaken =>
                        AuthErrorCodes.UsernameTaken,

                    _ =>
                        AuthErrorCodes.GoogleSignupFailed
                };

            return "/register?error="
                + Uri.EscapeDataString(
                    normalizedCode);
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
            "/demo/mangaflow/editorial",

        "Editorial Board Chief" =>
            "/demo/mangaflow/editorial",

        _ =>
            "/access-denied"
    };

        private static async Task<IResult> SignInAndRedirectAsync(
            HttpContext context,
            LoginApiResult loginResult)
        {
            await SignInApplicationUserAsync(
                context,
                loginResult.User,
                loginResult.RoleName,
                loginResult.AccessToken,
                loginResult.ExpiresAtUtc);

            return Results.Redirect(
                GetDashboardRedirectUrl(
                    loginResult.RoleName));
        }
    }
}
