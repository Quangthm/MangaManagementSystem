using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record ChapterEditorialReviewDto(
        long ChapterEditorialReviewId,
        long ChapterId,
        int ReviewerUserId,
        string DecisionCode,
        string? Feedback,
        long? MarkupFileId
    );

    public record CreateChapterEditorialReviewDto(
        [Required] long ChapterId,
        [Required] int ReviewerUserId,
        [Required][MaxLength(30)] string DecisionCode,
        string? Feedback,
        long? MarkupFileId
    );

    public record UpdateChapterEditorialReviewDto(
        [Required] long ChapterEditorialReviewId,
        [Required] long ChapterId,
        [Required] int ReviewerUserId,
        [Required][MaxLength(30)] string DecisionCode,
        string? Feedback,
        long? MarkupFileId
    );
}
