using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record SeriesProposalDto(
        Guid SeriesProposalId,
        Guid SeriesId,
        short ProposalVersionNo,
        string ProposalTitle,
        string SynopsisSnapshot,
        IReadOnlyList<GenreDto> Genres,
        IReadOnlyList<TagDto> Tags,
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
        IReadOnlyList<GenreDto> Genres,
        IReadOnlyList<TagDto> Tags,
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

    /// <summary>
    /// Read-only proposal detail for the Tantou Editor review screen. Snapshot fields are
    /// immutable (no editing). Permission flags are computed server-side for the current actor
    /// so the UI can show/hide actions without re-deriving business rules.
    /// </summary>
    public sealed record EditorProposalDetailDto(
        Guid SeriesProposalId,
        Guid SeriesId,
        string SeriesTitle,
        string SeriesSlug,
        string? SeriesCoverUrl,
        short ProposalVersionNo,
        string ProposalTitle,
        IReadOnlyList<GenreDto> Genres,
        IReadOnlyList<TagDto> Tags,
        string SynopsisSnapshot,
        string ProposalStatusCode,
        string? SeriesStatusCode,
        Guid SubmittedByUserId,
        string SubmitterDisplayName,
        DateTime SubmittedAtUtc,
        Guid? ReviewedByUserId,
        string? ReviewerDisplayName,
        DateTime? ReviewedAtUtc,
        string? Comments,
        Guid ProposalFileId,
        string? ProposalFileName,
        string? ProposalFileUrl,
        Guid? MarkupFileId,
        string? MarkupFileName,
        string? MarkupFileUrl,
        // Permission / state flags (computed for the current actor)
        bool CurrentActorIsActiveTantouEditorContributor,
        bool CurrentActorHasClaimed,
        bool HasEditorialDecision,
        bool CanClaim,
        bool CanRequestRevision,
        bool CanPassToBoard,
        bool CanCancel
    );

    /// <summary>
    /// Result of an editorial review action. Returns the proposal's resulting status code so
    /// the UI can refresh and confirm the transition.
    /// </summary>
    public sealed record EditorReviewActionResultDto(
        Guid SeriesProposalId,
        string ProposalStatusCode
    );

    /// <summary>
    /// Read-only proposal tracking DTO for Mangaka users. Shows scoped proposals with
    /// series info and file references for the tracking dashboard.
    /// </summary>
    public sealed record MangakaSeriesProposalDto(
        Guid SeriesProposalId,
        Guid SeriesId,
        string SeriesSlug,
        string SeriesTitle,
        short ProposalVersionNo,
        string ProposalTitle,
        string SynopsisSnapshot,
        IReadOnlyList<GenreDto> Genres,
        IReadOnlyList<TagDto> Tags,
        string StatusCode,
        DateTime SubmittedAtUtc,
        DateTime? WithdrawnAtUtc,
        DateTime? ReviewedAtUtc,
        string? Comments,
        string SubmittedByDisplayName,
        string? ReviewedByDisplayName,
        ProposalFileRefDto ProposalFile,
        ProposalFileRefDto? MarkupFile);

    /// <summary>
    /// Lean file reference for API-facing DTOs. Maps FileResource.CloudinarySecureUrl → SecureUrl
    /// to avoid exposing Cloudinary naming in the UI layer.
    /// </summary>
    public sealed record ProposalFileRefDto(
        Guid FileResourceId,
        string OriginalFileName,
        string ContentType,
        long FileSizeBytes,
        string SecureUrl);
}
