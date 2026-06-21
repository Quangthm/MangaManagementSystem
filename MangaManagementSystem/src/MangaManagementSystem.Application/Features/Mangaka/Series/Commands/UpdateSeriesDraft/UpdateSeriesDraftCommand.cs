using System;
using System.Collections.Generic;
using MangaManagementSystem.Application.DTOs.Manga;
using MediatR;

namespace MangaManagementSystem.Application.Features.Mangaka.Series.Commands.UpdateSeriesDraft
{
    /// <summary>
    /// CQRS write command for BF-SERIES-002 — Edit Series Draft Profile.
    /// The handler owns: input validation, optional Cloudinary cover upload,
    /// Cloudinary cleanup on SQL failure, and the stored-procedure call through
    /// ISeriesRepository. Cover upload is optional; pass null cover fields to keep
    /// the existing cover unchanged.
    ///
    /// Only PROPOSAL_DRAFT series can be updated. The stored procedure enforces the
    /// status guard, contributor permission, and audit event.
    /// </summary>
    public sealed record UpdateSeriesDraftCommand(
        Guid ActorUserId,
        Guid SeriesId,
        string Title,
        string Synopsis,
        IReadOnlyList<Guid> GenreIds,
        IReadOnlyList<Guid> TagIds,
        string ContentLanguageCode,
        string? PublicationFrequencyCode,
        string? Slug,
        // Optional new cover — all null = keep existing cover
        byte[]? CoverFileBytes,
        string? CoverFileName,
        string? CoverContentType) : IRequest<SeriesDraftUpdatedDto>;
}
