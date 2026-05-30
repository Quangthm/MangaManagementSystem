using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record SeriesProposalDto(
        long SeriesProposalId,
        long SeriesId,
        short ProposalVersionNo,
        string? ProposalTitle,
        string? SynopsisSnapshot,
        string? GenreSnapshot,
        long? ProposalFileId,
        string StatusCode,
        int? SubmittedByUserId,
        int? ReviewedByUserId,
        long? MarkupFileId
    );

    public record CreateSeriesProposalDto(
        [Required] long SeriesId,
        [Required] short ProposalVersionNo,
        string? ProposalTitle,
        string? SynopsisSnapshot,
        string? GenreSnapshot,
        long? ProposalFileId,
        [Required][MaxLength(50)] string StatusCode,
        int? SubmittedByUserId,
        int? ReviewedByUserId,
        long? MarkupFileId
    );

    public record UpdateSeriesProposalDto(
        [Required] long SeriesProposalId,
        [Required] long SeriesId,
        [Required] short ProposalVersionNo,
        string? ProposalTitle,
        string? SynopsisSnapshot,
        string? GenreSnapshot,
        long? ProposalFileId,
        [Required][MaxLength(50)] string StatusCode,
        int? SubmittedByUserId,
        int? ReviewedByUserId,
        long? MarkupFileId
    );
}
