using System;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record SeriesProposalDto(
        Guid SeriesProposalId,
        Guid SeriesId,
        short ProposalVersionNo,
        string ProposalTitle,
        string SynopsisSnapshot,
        string GenreSnapshot,
        Guid ProposalFileId,
        string StatusCode,
        Guid SubmittedByUserId,
        DateTime SubmittedAtUtc,
        DateTime? WithdrawnAtUtc,
        Guid? ReviewedByUserId,
        DateTime? ReviewedAtUtc,
        string? Comments,
        Guid? MarkupFileId,
        bool HasActiveTantouEditor = false
    );

    public record ProposalQueueItemDto(
        Guid SeriesProposalId,
        Guid SeriesId,
        string SeriesTitle,
        string SeriesSlug,
        short ProposalVersionNo,
        string ProposalTitle,
        string SynopsisSnapshot,
        string GenreSnapshot,
        string StatusCode,
        Guid SubmittedByUserId,
        string SubmitterDisplayName,
        DateTime SubmittedAtUtc,
        Guid? ReviewedByUserId,
        string? ReviewerDisplayName,
        DateTime? ReviewedAtUtc,
        string? Comments,
        Guid ProposalFileId,
        string? ProposalFileUrl,
        string? ProposalFileName,
        Guid? MarkupFileId,
        string? MarkupFileUrl
    );

    public record ProposalQueueFilterDto(
        string? StatusCode = null,
        Guid? SeriesId = null,
        Guid? SubmittedByUserId = null,
        Guid? ReviewedByUserId = null
    );

    public record CreateProposalDto(
        [Required] Guid SeriesId,
        [Required][MaxLength(200)] string ProposalTitle,
        [Required] string SynopsisSnapshot,
        [Required][MaxLength(100)] string GenreSnapshot,
        [Required] Guid ProposalFileId,
        [Required] Guid SubmittedByUserId
    );

    public record ProposalReviewRequestDto(
        [Required] Guid SeriesProposalId,
        [Required] string Comments
    );
}
