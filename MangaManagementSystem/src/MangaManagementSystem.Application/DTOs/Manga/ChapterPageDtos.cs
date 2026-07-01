using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record ChapterPageDto(
        Guid ChapterPageId,
        Guid ChapterId,
        int PageNo,
        string? PageNotes,
        DateTime? DeletedAtUtc,
        Guid? DeletedByUserId
    );

    public record CreateChapterPageDto(
        [Required] Guid ChapterId,
        [Required] int PageNo,
        string? PageNotes
    );

    public record UpdateChapterPageDto(
        [Required] Guid ChapterPageId,
        [Required] Guid ChapterId,
        [Required] int PageNo,
        string? PageNotes
    );

    /// <summary>Web → API body to update a page's whole-page note.</summary>
    public sealed record UpdatePageNotesRequest(string? PageNotes);

    /// <summary>Web → API body to fetch non-deleted page counts for several chapters.</summary>
    public sealed record PageCountsRequest(IReadOnlyList<Guid> ChapterIds);
}
