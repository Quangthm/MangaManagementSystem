using MangaManagementSystem.Domain.Common;
using System;

namespace MangaManagementSystem.Domain.Entities
{
public class ChapterEditorialReview : BaseEntity
{
    public Guid ChapterEditorialReviewId { get; set; }
    public Guid ChapterId { get; set; }
    public Chapter? Chapter { get; set; }
    public Guid ReviewerUserId { get; set; }
    public User? ReviewerUser { get; set; }
        public string DecisionCode { get; set; } = null!;
        public string? Feedback { get; set; }
        public Guid? MarkupFileId { get; set; }
        public FileResource? MarkupFile { get; set; }
        public DateTime ReviewedAtUtc { get; set; }
    }
}
