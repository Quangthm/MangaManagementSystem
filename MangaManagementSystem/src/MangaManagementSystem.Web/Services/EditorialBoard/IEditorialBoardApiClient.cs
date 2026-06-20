namespace MangaManagementSystem.Web.Services.EditorialBoard;

public interface IEditorialBoardApiClient
{
    Task<EditorialDashboardDto?> GetDashboardAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EditorialBoardPollDto>> GetOpenPollsAsync(
        CancellationToken cancellationToken = default);

    Task<OpenPollResult?> OpenPollAsync(
        Guid proposalId,
        OpenPollRequest request,
        CancellationToken cancellationToken = default);

    Task<CastVoteResult?> CastVoteAsync(
        Guid pollId,
        CastVoteRequest request,
        CancellationToken cancellationToken = default);
}