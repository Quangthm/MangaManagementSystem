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
        Guid? MarkupFileId
    );

    public record CreateSeriesProposalDto(
        [Required] Guid SeriesId,
        [Required] short ProposalVersionNo,
        [Required][MaxLength(200)] string ProposalTitle,
        [Required] string SynopsisSnapshot,
        [Required][MaxLength(100)] string GenreSnapshot,
        [Required] Guid ProposalFileId,
        [Required][MaxLength(50)] string StatusCode,
        [Required] Guid SubmittedByUserId,
        string? Comments,
        Guid? MarkupFileId
    );

    public record UpdateSeriesProposalDto(
        [Required] Guid SeriesProposalId,
        [Required] Guid SeriesId,
        [Required] short ProposalVersionNo,
        [Required][MaxLength(200)] string ProposalTitle,
        [Required] string SynopsisSnapshot,
        [Required][MaxLength(100)] string GenreSnapshot,
        [Required] Guid ProposalFileId,
        [Required][MaxLength(50)] string StatusCode,
        [Required] Guid SubmittedByUserId,
        Guid? ReviewedByUserId,
        DateTime? ReviewedAtUtc,
        string? Comments,
        Guid? MarkupFileId
    );
}
