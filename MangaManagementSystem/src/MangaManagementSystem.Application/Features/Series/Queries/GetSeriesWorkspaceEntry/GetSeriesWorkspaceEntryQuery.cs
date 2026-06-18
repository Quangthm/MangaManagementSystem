using System;
using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.Series.Queries.GetSeriesWorkspaceEntry
{
    public sealed record GetSeriesWorkspaceEntryQuery(
        string Slug,
        Guid ActorUserId) : IRequest<SeriesWorkspaceEntryDto?>;
}
