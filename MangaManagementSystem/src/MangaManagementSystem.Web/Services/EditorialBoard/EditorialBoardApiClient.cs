using System.Net.Http.Json;

namespace MangaManagementSystem.Web.Services.EditorialBoard;

public sealed class EditorialBoardApiClient : IEditorialBoardApiClient
{
    private readonly HttpClient _httpClient;

    public EditorialBoardApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<EditorialDashboardDto?> GetDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<EditorialDashboardDto>(
            "api/editorial-board/dashboard",
            cancellationToken);
    }

    public async Task<IReadOnlyList<EditorialBoardPollDto>> GetOpenPollsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<IReadOnlyList<EditorialBoardPollDto>>(
            "api/editorial-board/polls/open",
            cancellationToken);

        return result ?? Array.Empty<EditorialBoardPollDto>();
    }

    public async Task<OpenPollResult?> OpenPollAsync(
        Guid proposalId,
        OpenPollRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/editorial-board/proposals/{proposalId}/polls",
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(message);
        }

        return await response.Content.ReadFromJsonAsync<OpenPollResult>(
            cancellationToken: cancellationToken);
    }

    public async Task<CastVoteResult?> CastVoteAsync(
        Guid pollId,
        CastVoteRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/editorial-board/polls/{pollId}/votes",
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(message);
        }

        return await response.Content.ReadFromJsonAsync<CastVoteResult>(
            cancellationToken: cancellationToken);
    }
        public async Task<FinalizePollResult?> FinalizeApprovalAsync(
    Guid pollId,
    CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/editorial-board/polls/{pollId}/final-approval",
            new { },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(message);
        }

        return await response.Content.ReadFromJsonAsync<FinalizePollResult>(
            cancellationToken: cancellationToken);
    }
}