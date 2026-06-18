using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.SeriesProposals.Commands.PassProposalToBoard
{
    /// <summary>
    /// Editorial decision: Pass To Board (FR-PROP-014).
    /// Comments optional, markup optional. Moves the proposal to UNDER_BOARD_REVIEW.
    /// Must NOT mark the proposal APPROVED — approval only comes from the board result.
    ///
    /// Markup is passed as raw bytes + name + content type so the Application layer does not
    /// depend on ASP.NET Core HTTP types. The handler performs the Cloudinary upload.
    /// </summary>
    public sealed record PassProposalToBoardCommand(
        Guid ActorUserId,
        Guid SeriesProposalId,
        string? Comments,
        byte[]? MarkupFileBytes,
        string? MarkupFileName,
        string? MarkupContentType) : IRequest<EditorReviewActionResultDto>;
}
