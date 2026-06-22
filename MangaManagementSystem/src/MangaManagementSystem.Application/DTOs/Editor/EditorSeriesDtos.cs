using System;
using System.Collections.Generic;

namespace MangaManagementSystem.Application.DTOs.Editor
{
    public sealed record EditorSeriesDto(
        Guid SeriesId,
        string Title,
        string Slug,
        string StatusCode,
        DateTime CreatedAtUtc,
        Guid? LatestProposalId,
        string? LatestProposalStatusCode,
        bool CanOpenSeriesSlugPage);

    public sealed record EditorSeriesListDto(
        IReadOnlyList<EditorSeriesDto> Series);
}
