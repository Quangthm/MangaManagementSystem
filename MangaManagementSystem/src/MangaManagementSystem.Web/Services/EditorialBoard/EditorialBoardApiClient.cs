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

    public async Task<IReadOnlyList<EditorialBoardPollDto>> GetPollHistoryAsync(
    CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<IReadOnlyList<EditorialBoardPollDto>>(
            "api/editorial-board/polls/history",
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

    public async Task<IReadOnlyList<CancellableBoardSeriesDto>> GetCancellableSeriesForCancelPollAsync(
    CancellationToken cancellationToken = default)
    {
        var result = await _httpClient.GetFromJsonAsync<IReadOnlyList<CancellableBoardSeriesDto>>(
            "api/editorial-board/series/cancellable-for-cancel-poll",
            cancellationToken);

        return result ?? Array.Empty<CancellableBoardSeriesDto>();
    }

    public async Task<OpenPollResult?> OpenCancelSerializationPollAsync(
        Guid seriesId,
        OpenCancelSerializationPollRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/editorial-board/series/{seriesId}/cancel-poll",
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

    public async Task<FinalizePollResult?> CancelPollAsync(
        Guid pollId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/editorial-board/polls/{pollId}/cancel",
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


    public async Task<UpdateBoardPollDeadlineResult?> UpdatePollDeadlineAsync(
        Guid pollId,
        UpdateBoardPollDeadlineRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PatchAsJsonAsync(
            $"api/editorial-board/polls/{pollId}/deadline",
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(message);
        }

        return await response.Content.ReadFromJsonAsync<UpdateBoardPollDeadlineResult>(
            cancellationToken: cancellationToken);
    }

    public async Task<UpdateSeriesPublicationFrequencyResult?> UpdateSeriesPublicationFrequencyAsync(
    Guid seriesId,
    UpdateSeriesPublicationFrequencyRequest request,
    CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PatchAsJsonAsync(
            $"api/editorial-board/series/{seriesId}/publication-frequency",
            request,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(message);
        }

        return await response.Content.ReadFromJsonAsync<UpdateSeriesPublicationFrequencyResult>(
            cancellationToken: cancellationToken);
    }
}
