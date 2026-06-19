using System;
using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.SeriesProposals.Commands.ClaimEditorialReview
{
    /// <summary>
    /// Claims a submitted proposal for editorial review. Claim is represented by inserting the
    /// acting Tantou Editor as an active SeriesContributor (handled by the stored procedure).
    /// No file upload is involved.
    /// </summary>
    public sealed record ClaimEditorialReviewCommand(
        Guid ActorUserId,
        Guid SeriesProposalId,
        string? Notes) : IRequest<EditorReviewActionResultDto>;
}
