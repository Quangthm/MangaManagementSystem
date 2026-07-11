using System.Net.Http.Json;
using System.Text.Json;
using MangaManagementSystem.Application.DTOs.Auth;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.Web.Services.Api;

public sealed class AuthApiClient : BaseApiClient, IAuthApiClient
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

        var message = await ExtractErrorMessageAsync(response, "Invalid credentials", cancellationToken);

        _logger.LogWarning(
            "Login failed: {StatusCode} {ReasonPhrase}",
            (int)response.StatusCode,
            response.ReasonPhrase);

        throw new InvalidOperationException(message);
    }


}