namespace MangaManagementSystem.Web.Services.Api;

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

    Task<FinalizePollResult?> FinalizeApprovalAsync(
        Guid pollId,
        CancellationToken cancellationToken = default);

    Task<FinalizePollResult?> CancelPollAsync(
        Guid pollId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EditorialBoardPollDto>> GetPollHistoryAsync(
    CancellationToken cancellationToken = default);
}
