using System;
using System.Collections.Generic;
using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.SeriesProposals.Queries.GetMySeriesProposals
{
    /// <summary>
    /// Returns all series proposals scoped to the requesting Mangaka user's active contributor
    /// memberships. Read-only tracking query — no mutations.
    /// </summary>
    public sealed record GetMySeriesProposalsQuery(Guid ActorUserId)
        : IRequest<IReadOnlyList<MangakaSeriesProposalDto>>;
}
