using MangaManagementSystem.Domain.Common;
using System;

namespace MangaManagementSystem.Domain.Entities
{
    public class ChapterEditorialReview : BaseEntity
    {
        public long ChapterEditorialReviewId { get; set; }
        public long ChapterId { get; set; }
        public Chapter? Chapter { get; set; }
        public int ReviewerUserId { get; set; }
        public User? ReviewerUser { get; set; }
        public string DecisionCode { get; set; } = null!;
        public string? Feedback { get; set; }
        public long? MarkupFileId { get; set; }
        public FileResource? MarkupFile { get; set; }
    }
}
