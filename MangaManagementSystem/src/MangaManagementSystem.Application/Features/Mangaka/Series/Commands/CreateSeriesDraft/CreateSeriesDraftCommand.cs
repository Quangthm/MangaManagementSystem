using System;
using System.Collections.Generic;
using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.Series.Commands.CreateSeriesDraft
{
    /// <summary>
    /// CQRS write command for Create Series Draft (BF-SERIES-001).
    /// Migrated from the transitional ISeriesService.CreateSeriesDraftAsync path.
    ///
    /// The handler owns: input validation, optional cover file validation and Cloudinary upload,
    /// SHA-256 null guard, stored-procedure call through ISeriesRepository, and best-effort
    /// Cloudinary cleanup on SQL failure.
    ///
    /// Must NOT create SeriesProposal, SERIES_PROPOSAL FileResource, or any editorial record.
    /// Cover is SERIES_COVER only. Proposal submission remains a separate workflow.
    /// </summary>
    public sealed record CreateSeriesDraftCommand(
        Guid ActorUserId,
        string Title,
        string Synopsis,
        IReadOnlyList<Guid> GenreIds,
        IReadOnlyList<Guid> TagIds,
        string? ContentLanguageCode,
        string? Slug,
        string? PublicationFrequencyCode,
        Guid? SourceSeriesId,
        // Optional cover — all null means no cover
        byte[]? CoverFileBytes,
        string? CoverFileName,
        string? CoverContentType) : IRequest<SeriesDraftCreatedDto>;
}
