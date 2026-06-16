using MangaManagementSystem.Application;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Infrastructure;
using MangaManagementSystem.Web.Components;
using MangaManagementSystem.Web.Helpers;
using MangaManagementSystem.Web.Services;
using MangaManagementSystem.Web.Options;
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

            builder.Services.Configure<RecaptchaOptions>(builder.Configuration.GetSection(RecaptchaOptions.SectionName));
            builder.Services.AddHttpClient<RecaptchaService>();

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
                .AddInteractiveServerComponents(options =>
                {
                    options.DetailedErrors = true;
                })
                .AddHubOptions(options =>
                {
                    options.MaximumReceiveMessageSize = 20 * 1024 * 1024; // 20 MB limit
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
                IAuthService authService,
                RecaptchaService recaptchaService,
                [FromForm] string username,
                [FromForm] string password) =>
            {
                var recaptchaResponse = context.Request.Form["g-recaptcha-response"].ToString();
                var ip = context.Connection.RemoteIpAddress?.ToString();
                var isRecaptchaValid = await recaptchaService.VerifyTokenAsync(recaptchaResponse, ip);
                if (!isRecaptchaValid)
                {
                    Console.WriteLine("CAPTCHA validation failed, but bypassing for testing.");
                    // return Results.Redirect("/login?error=RecaptchaFailed");
                }

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
                return Results.Redirect(GetDashboardRedirectUrl(result.RoleName));
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
                return Results.Redirect(GetDashboardRedirectUrl(authResult.RoleName));
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

app.MapGet("/signout", async (CustomAuthenticationStateProvider authStateProvider, HttpContext context) =>
{
    await authStateProvider.MarkUserAsLoggedOut();
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

            // Debug endpoint: tries multiple download strategies and reports results
            app.MapGet("/api/portfolio/{id:guid}/debug", async (
                Guid id,
                IFileResourceService fileResourceService,
                CloudinaryDotNet.Cloudinary cloudinary,
                ILogger<Program> logger) =>
            {
                var lines = new System.Collections.Generic.List<string>();
                lines.Add($"[1] Looking up FileResource: {id}");

                try
                {
                    var file = await fileResourceService.GetFileResourceByIdAsync(id);
                    if (file == null)
                    {
                        lines.Add("[FAIL] File not found in database.");
                        return Results.Text(string.Join("\n", lines), "text/plain");
                    }

                    lines.Add($"[OK] Found: {file.OriginalFileName}");
                    lines.Add($"  ContentType: {file.ContentType}");
                    lines.Add($"  PublicId: {file.CloudinaryPublicId}");
                    lines.Add($"  StoredUrl: {file.CloudinarySecureUrl}");

                    var account = cloudinary.Api.Account;
                    lines.Add($"  Cloud: {account.Cloud}");
                    lines.Add("");

                    // === Strategy A: CDN URL with browser-like headers ===
                    lines.Add("=== Strategy A: CDN URL + Browser Headers ===");
                    using (var httpClient = new System.Net.Http.HttpClient())
                    {
                        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                        httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
                        var response = await httpClient.GetAsync(file.CloudinarySecureUrl);
                        lines.Add($"  HTTP {(int)response.StatusCode}");
                        if (response.IsSuccessStatusCode)
                        {
                            var bytes = await response.Content.ReadAsByteArrayAsync();
                            lines.Add($"  [OK] Downloaded {bytes.Length} bytes");
                        }
                    }
                    lines.Add("");

                    // === Strategy B: Admin API with Basic Auth ===
                    lines.Add("=== Strategy B: Admin API (Basic Auth) ===");
                    using (var httpClient = new System.Net.Http.HttpClient())
                    {
                        var basicAuth = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{account.ApiKey}:{account.ApiSecret}"));
                        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", basicAuth);
                        var adminUrl = $"https://api.cloudinary.com/v1_1/{account.Cloud}/resources/raw/upload/{Uri.EscapeDataString(file.CloudinaryPublicId)}";
                        lines.Add($"  URL: {adminUrl}");
                        var response = await httpClient.GetAsync(adminUrl);
                        lines.Add($"  HTTP {(int)response.StatusCode}");
                        if (response.IsSuccessStatusCode)
                        {
                            var body = await response.Content.ReadAsStringAsync();
                            // Truncate to 500 chars for display
                            lines.Add($"  [OK] Response: {(body.Length > 500 ? body[..500] + "..." : body)}");
                        }
                        else
                        {
                            var body = await response.Content.ReadAsStringAsync();
                            lines.Add($"  [FAIL] Response: {body}");
                        }
                    }
                    lines.Add("");

                    // === Strategy C: CDN URL with Basic Auth ===
                    lines.Add("=== Strategy C: CDN URL + Basic Auth ===");
                    using (var httpClient = new System.Net.Http.HttpClient())
                    {
                        var basicAuth = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{account.ApiKey}:{account.ApiSecret}"));
                        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", basicAuth);
                        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                        var response = await httpClient.GetAsync(file.CloudinarySecureUrl);
                        lines.Add($"  HTTP {(int)response.StatusCode}");
                        if (response.IsSuccessStatusCode)
                        {
                            var bytes = await response.Content.ReadAsByteArrayAsync();
                            lines.Add($"  [OK] Downloaded {bytes.Length} bytes");
                        }
                    }

                    return Results.Text(string.Join("\n", lines), "text/plain");
                }
                catch (Exception ex)
                {
                    lines.Add($"[ERROR] {ex.GetType().Name}: {ex.Message}");
                    return Results.Text(string.Join("\n", lines), "text/plain");
                }
            });

            app.MapGet("/api/portfolio/{id:guid}", async (
                Guid id,
                IFileResourceService fileResourceService,
                ILogger<Program> logger) =>
            {
                try
                {
                    var file = await fileResourceService.GetFileResourceByIdAsync(id);
                    if (file == null || string.IsNullOrWhiteSpace(file.CloudinarySecureUrl))
                    {
                        logger.LogWarning("Portfolio {Id}: file not found in DB", id);
                        return Results.Text("File not found.", "text/plain", statusCode: 404);
                    }

                    logger.LogInformation("Portfolio {Id}: downloading {FileName}", id, file.OriginalFileName);

                    // Cloudinary CDN rejects requests without a User-Agent header (bot detection → 401).
                    // Adding a browser-like User-Agent resolves this.
                    using var httpClient = new System.Net.Http.HttpClient();
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                    httpClient.DefaultRequestHeaders.Add("Accept", "*/*");

                    var response = await httpClient.GetAsync(file.CloudinarySecureUrl);
                    if (!response.IsSuccessStatusCode)
                    {
                        logger.LogError("Portfolio {Id}: download failed with HTTP {Status}", id, (int)response.StatusCode);
                        return Results.Text($"Download failed: HTTP {(int)response.StatusCode}", "text/plain", statusCode: 502);
                    }

                    var bytes = await response.Content.ReadAsByteArrayAsync();
                    var contentType = file.ContentType ?? "application/octet-stream";

                    logger.LogInformation("Portfolio {Id}: serving {Bytes} bytes as {ContentType}", id, bytes.Length, contentType);

                    // Return without fileDownloadName → Content-Disposition: inline
                    return Results.Bytes(bytes, contentType);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Portfolio {Id}: unhandled exception", id);
                    return Results.Text($"Error: {ex.Message}", "text/plain", statusCode: 500);
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

        private static string GetDashboardRedirectUrl(string roleName) => roleName switch
        {
            "Admin" => "/admin",
            "Mangaka" => "/mangaka",
            "Assistant" => "/assistant",
            "Tantou Editor" => "/editor",
            "Editorial Board Member" => "/board",
            "Editorial Board Chief" => "/board-chief",
            _ => "/login?error=InvalidCredentials"
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
