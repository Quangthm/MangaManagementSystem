using System;
using System.Collections.Generic;
using MangaManagementSystem.Application.DTOs.Manga;

namespace MangaManagementSystem.Web.Components.Shared
{
    /// <summary>
    /// Web-layer projection for the shared proposal table. Both Editor and Mangaka
    /// pages map their role-specific DTOs to this model before passing it to
    /// <see cref="SeriesProposalTable"/>.
    /// </summary>
    public sealed record SeriesProposalListItemModel(
        Guid SeriesProposalId,
        string SeriesTitle,
        string ProposalTitle,
        short ProposalVersionNo,
        string SubmittedByDisplayName,
        DateTime SubmittedAtUtc,
        string? ReviewerDisplayName,
        DateTime? ReviewedAtUtc,
        string StatusCode,
        bool HasMarkup,
        IReadOnlyList<GenreDto> Genres,
        IReadOnlyList<TagDto> Tags
    );
}
