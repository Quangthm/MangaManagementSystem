using System;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record ChapterReaderVoteSnapshotDto(
        Guid ChapterReaderVoteSnapshotId,
        Guid ChapterId,
        int ReaderVoteCount,
        decimal? AverageRating,
        int? PositiveFeedbackCount,
        int? NegativeFeedbackCount,
        string? DataSourceNote,
        Guid EnteredByUserId,
        DateTime VotedAtUtc
    );

    public record CreateChapterReaderVoteSnapshotDto(
        [Required] Guid ChapterId,
        [Required] int ReaderVoteCount,
        decimal? AverageRating,
        int? PositiveFeedbackCount,
        int? NegativeFeedbackCount,
        string? DataSourceNote,
        [Required] Guid EnteredByUserId
    );
}
