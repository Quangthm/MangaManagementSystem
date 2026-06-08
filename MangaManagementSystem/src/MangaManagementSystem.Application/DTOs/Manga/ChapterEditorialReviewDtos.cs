using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record ChapterEditorialReviewDto(
        Guid ChapterEditorialReviewId,
        Guid ChapterId,
        Guid ReviewerUserId,
        string DecisionCode,
        string? Feedback,
        Guid? MarkupFileId
    );

    public record CreateChapterEditorialReviewDto(
        [Required] Guid ChapterId,
        [Required] Guid ReviewerUserId,
        [Required][MaxLength(30)] string DecisionCode,
        string? Feedback,
        Guid? MarkupFileId
    );

    public record UpdateChapterEditorialReviewDto(
        [Required] Guid ChapterEditorialReviewId,
        [Required] Guid ChapterId,
        [Required] Guid ReviewerUserId,
        [Required][MaxLength(30)] string DecisionCode,
        string? Feedback,
        Guid? MarkupFileId
    );
}
