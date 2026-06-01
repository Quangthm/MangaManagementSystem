using System;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record SeriesProposalDto(
        long SeriesProposalId,
        long SeriesId,
        short ProposalVersionNo,
        string ProposalTitle,
        string SynopsisSnapshot,
        string GenreSnapshot,
        long ProposalFileId,
        string StatusCode,
        int SubmittedByUserId,
        DateTime SubmittedAtUtc,
        DateTime? WithdrawnAtUtc,
        int? ReviewedByUserId,
        DateTime? ReviewedAtUtc,
        string? Comments,
        long? MarkupFileId
    );

    public record CreateSeriesProposalDto(
        [Required] long SeriesId,
        [Required] short ProposalVersionNo,
        [Required][MaxLength(200)] string ProposalTitle,
        [Required] string SynopsisSnapshot,
        [Required][MaxLength(100)] string GenreSnapshot,
        [Required] long ProposalFileId,
        [Required][MaxLength(50)] string StatusCode,
        [Required] int SubmittedByUserId,
        string? Comments,
        long? MarkupFileId
    );

    public record UpdateSeriesProposalDto(
        [Required] long SeriesProposalId,
        [Required] long SeriesId,
        [Required] short ProposalVersionNo,
        [Required][MaxLength(200)] string ProposalTitle,
        [Required] string SynopsisSnapshot,
        [Required][MaxLength(100)] string GenreSnapshot,
        [Required] long ProposalFileId,
        [Required][MaxLength(50)] string StatusCode,
        [Required] int SubmittedByUserId,
        int? ReviewedByUserId,
        DateTime? ReviewedAtUtc,
        string? Comments,
        long? MarkupFileId
    );
}
