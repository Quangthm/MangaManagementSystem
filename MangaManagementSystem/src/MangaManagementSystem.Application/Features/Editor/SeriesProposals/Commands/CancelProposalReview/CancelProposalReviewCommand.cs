using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.SeriesProposals.Commands.CancelProposalReview
{
    /// <summary>
    /// Editorial decision: Cancel Proposal (FR-PROP-016A).
    /// Requires non-empty comments AND a markup file. Moves the proposal to CANCELLED
    /// (the stored procedure also cancels the parent series).
    ///
    /// Markup is passed as raw bytes + name + content type so the Application layer does not
    /// depend on ASP.NET Core HTTP types. The handler performs the Cloudinary upload.
    /// </summary>
    public sealed record CancelProposalReviewCommand(
        Guid ActorUserId,
        Guid SeriesProposalId,
        string Comments,
        byte[]? MarkupFileBytes,
        string? MarkupFileName,
        string? MarkupContentType) : IRequest<EditorReviewActionResultDto>;
}
