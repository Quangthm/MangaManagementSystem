using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record ChapterReaderVoteSnapshotDto(
        long ChapterReaderVoteSnapshotId,
        long ChapterId,
        int ReaderVoteCount,
        decimal AverageRating,
        int? EnteredByUserId
    );

    public record CreateChapterReaderVoteSnapshotDto(
        [Required] long ChapterId,
        [Required] int ReaderVoteCount,
        [Required] decimal AverageRating,
        int? EnteredByUserId
    );
}
