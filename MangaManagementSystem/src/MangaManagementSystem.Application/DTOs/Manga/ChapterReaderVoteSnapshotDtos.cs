using System;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record ChapterReaderVoteSnapshotDto(
        long ChapterReaderVoteSnapshotId,
        long ChapterId,
        int ReaderVoteCount,
        decimal? AverageRating,
        int? PositiveFeedbackCount,
        int? NegativeFeedbackCount,
        string? DataSourceNote,
        int EnteredByUserId,
        DateTime VotedAtUtc
    );

    public record CreateChapterReaderVoteSnapshotDto(
        [Required] long ChapterId,
        [Required] int ReaderVoteCount,
        decimal? AverageRating,
        int? PositiveFeedbackCount,
        int? NegativeFeedbackCount,
        string? DataSourceNote,
        [Required] int EnteredByUserId
    );
}
