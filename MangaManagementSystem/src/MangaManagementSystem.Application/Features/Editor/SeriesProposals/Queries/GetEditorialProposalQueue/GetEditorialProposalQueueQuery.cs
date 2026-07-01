using System;
using System.Collections.Generic;
using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.Editor.SeriesProposals.Queries.GetEditorialProposalQueue
{
    /// <summary>
    /// Read-only query for the Tantou Editor proposal review queue.
    /// Optional status filter narrows the queue (e.g. UNDER_EDITORIAL_REVIEW).
    /// ActorUserId is used to compute CurrentEditorIsActiveContributor per proposal.
    /// Backed by an EF Core AsNoTracking read; no mutations.
    /// </summary>
    public sealed record GetEditorialProposalQueueQuery(
        Guid ActorUserId,
        string? StatusCode) : IRequest<IReadOnlyList<ProposalQueueItemDto>>;
}
