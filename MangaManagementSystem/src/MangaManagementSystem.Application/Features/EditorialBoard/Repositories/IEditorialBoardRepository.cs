using MangaManagementSystem.Application.Features.EditorialBoard.Dtos;

namespace MangaManagementSystem.Application.Features.EditorialBoard.Repositories;

public interface IEditorialBoardRepository
{
    Task<EditorialDashboardDto> GetDashboardAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EditorialBoardPollDto>> GetOpenPollsAsync(
        Guid currentUserId,
        CancellationToken cancellationToken);

    Task<OpenSeriesBoardPollResultDto> OpenPollAsync(
        OpenSeriesBoardPollRequestDto request,
        Guid chiefUserId,
        CancellationToken cancellationToken);

    Task<CastSeriesBoardVoteResultDto> CastVoteAsync(
        CastSeriesBoardVoteRequestDto request,
        Guid voterUserId,
        CancellationToken cancellationToken);

    Task<FinalizeBoardPollResultDto> FinalizeApprovalAsync(
        Guid pollId,
        Guid chiefUserId,
        CancellationToken cancellationToken);

    Task<FinalizeBoardPollResultDto> CancelPollAsync(
        Guid pollId,
        Guid chiefUserId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EditorialBoardPollDto>> GetPollHistoryAsync(
    Guid currentUserId,
    CancellationToken cancellationToken);
}