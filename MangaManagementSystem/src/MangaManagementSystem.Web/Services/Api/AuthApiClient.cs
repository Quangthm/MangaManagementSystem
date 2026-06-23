using System.Net.Http.Json;
using System.Text.Json;
using MangaManagementSystem.Application.DTOs.Auth;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.Web.Services.Api;

public sealed class AuthApiClient : IAuthApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthApiClient> _logger;

    public AuthApiClient(
        HttpClient httpClient,
        ILogger<AuthApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<LoginApiResult> LoginAsync(
        string usernameOrEmail,
        string password,
        CancellationToken cancellationToken = default)
    {
        var request = new
        {
            UsernameOrEmail = usernameOrEmail,
            Password = password
        };

        var response = await _httpClient.PostAsJsonAsync(
            "api/auth/login",
            request,
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var loginResult = await response.Content.ReadFromJsonAsync<LoginApiResult>(
                cancellationToken: cancellationToken);

            return loginResult
                ?? throw new InvalidOperationException("Login response was empty.");
        }

        var message = await ExtractErrorMessageAsync(response, cancellationToken);

        _logger.LogWarning(
            "Login failed: {StatusCode} {ReasonPhrase}",
            (int)response.StatusCode,
            response.ReasonPhrase);

        throw new InvalidOperationException(message);
    }

    private static async Task<string> ExtractErrorMessageAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(body))
            {
                return "Invalid credentials";
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.TryGetProperty("message", out var msgProp)
                && msgProp.ValueKind == JsonValueKind.String)
            {
                var msg = msgProp.GetString();

                if (!string.IsNullOrWhiteSpace(msg))
                {
                    return msg;
                }
            }

            if (root.TryGetProperty("detail", out var detailProp)
                && detailProp.ValueKind == JsonValueKind.String)
            {
                var detail = detailProp.GetString();

                if (!string.IsNullOrWhiteSpace(detail))
                {
                    return detail;
                }
            }

            if (root.TryGetProperty("title", out var titleProp)
                && titleProp.ValueKind == JsonValueKind.String)
            {
                var title = titleProp.GetString();

                if (!string.IsNullOrWhiteSpace(title))
                {
                    return title;
                }
            }
        }
        catch (JsonException)
        {
        }

        return "Invalid credentials";
    }
}