using System;
using System.Collections.Generic;
using MangaManagementSystem.Application.DTOs.Manga;

namespace MangaManagementSystem.Web.Components.Shared
{
    /// <summary>
    /// Web-layer projection for the shared proposal detail view. Both Editor and
    /// Mangaka pages map their role-specific detail DTOs to this model before
    /// passing it to <see cref="SeriesProposalDetailView"/>.
    /// </summary>
    public sealed record SeriesProposalDetailViewModel(
        Guid SeriesProposalId,
        string SeriesTitle,
        string SeriesSlug,
        string? SeriesCoverUrl,
        short ProposalVersionNo,
        string ProposalTitle,
        string SynopsisSnapshot,
        IReadOnlyList<GenreDto> Genres,
        IReadOnlyList<TagDto> Tags,
        string StatusCode,
        string? SeriesStatusCode,
        string SubmittedByDisplayName,
        DateTime SubmittedAtUtc,
        string? ReviewerDisplayName,
        DateTime? ReviewedAtUtc,
        string? Comments,
        string? ProposalFileName,
        string? ProposalFileUrl,
        string? MarkupFileName,
        string? MarkupFileUrl,
        bool HasEditorialDecision
    );
}
