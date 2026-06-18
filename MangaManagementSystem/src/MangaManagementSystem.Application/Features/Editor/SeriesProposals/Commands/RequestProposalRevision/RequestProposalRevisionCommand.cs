using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.SeriesProposals.Commands.RequestProposalRevision
{
    /// <summary>
    /// Editorial decision: Request Revision (FR-PROP-016).
    /// Requires non-empty comments. Markup file is optional. Moves the proposal to
    /// REVISION_REQUESTED (the stored procedure also returns the series to PROPOSAL_DRAFT).
    ///
    /// Markup is passed as raw bytes + name + content type so the Application layer does not
    /// depend on ASP.NET Core HTTP types. The handler performs the Cloudinary upload.
    /// </summary>
    public sealed record RequestProposalRevisionCommand(
        Guid ActorUserId,
        Guid SeriesProposalId,
        string Comments,
        byte[]? MarkupFileBytes,
        string? MarkupFileName,
        string? MarkupContentType) : IRequest<EditorReviewActionResultDto>;
}
