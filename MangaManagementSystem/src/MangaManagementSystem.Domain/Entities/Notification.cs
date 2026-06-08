using MangaManagementSystem.Domain.Common;
using System;

namespace MangaManagementSystem.Domain.Entities
{
public class Notification : BaseEntity
{
    public Guid NotificationId { get; set; }
    public Guid RecipientUserId { get; set; }
    public User? RecipientUser { get; set; }
    public string NotificationTypeCode { get; set; } = "SYSTEM_MESSAGE";
        public string? Title { get; set; }
        public string Message { get; set; } = null!;
        public string? RelatedEntityType { get; set; }
        public Guid? RelatedEntityId { get; set; }
        public DateTime? ReadAtUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
