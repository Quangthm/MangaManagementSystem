using MangaManagementSystem.Domain.Common;
using System;

namespace MangaManagementSystem.Domain.Entities
{
    public class Notification : BaseEntity
    {
        public long NotificationId { get; set; }
        public int RecipientUserId { get; set; }
        public User? RecipientUser { get; set; }
        public string NotificationTypeCode { get; set; } = "SYSTEM_MESSAGE";
        public string? Title { get; set; }
        public string? Message { get; set; }
        public string? RelatedEntityType { get; set; }
        public string? RelatedEntityId { get; set; }
        public DateTime? ReadAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
