using MangaManagementSystem.Domain.Common;
using System;

namespace MangaManagementSystem.Domain.Entities
{
    public class AuditEvent : BaseEntity
    {
        public long AuditEventId { get; set; }
        public DateTime OccurredAtUtc { get; set; }
    public Guid? ActorUserId { get; set; }
    public User? ActorUser { get; set; }
        public string? ActorRoleName { get; set; }
        public string ActionCode { get; set; } = null!;
        public string EntityType { get; set; } = null!;
        public string? EntityId { get; set; }
        public string? DetailJson { get; set; }
    }
}
