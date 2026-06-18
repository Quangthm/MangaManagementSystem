using System;
using MediatR;
using MangaManagementSystem.Application.DTOs.Manga;

namespace MangaManagementSystem.Application.Features.Editor.SeriesProposals.Queries.GetEditorProposalDetail
{
    /// <summary>
    /// CQRS read query for the Tantou Editor proposal review detail screen.
    /// Returns the immutable proposal snapshot plus permission flags computed for the
    /// requesting actor. Returns null when the proposal does not exist.
    /// </summary>
    public sealed record GetEditorProposalDetailQuery(
        Guid SeriesProposalId,
        Guid ActorUserId) : IRequest<EditorProposalDetailDto?>;
}
