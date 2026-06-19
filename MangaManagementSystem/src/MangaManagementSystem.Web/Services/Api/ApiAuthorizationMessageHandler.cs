using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Authorization;

namespace MangaManagementSystem.Web.Services.Api;

public sealed class ApiAuthorizationMessageHandler : DelegatingHandler
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public ApiAuthorizationMessageHandler(
        AuthenticationStateProvider authenticationStateProvider)
    {
        _authenticationStateProvider = authenticationStateProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var authenticationState =
            await _authenticationStateProvider.GetAuthenticationStateAsync();

        var accessToken = authenticationState.User
            .FindFirst("api_access_token")
            ?.Value;

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}