using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaManagementSystem.Infrastructure.Persistence.Configurations
{
    public class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
    {
        public void Configure(EntityTypeBuilder<AuditEvent> builder)
        {
            builder.ToTable("AuditEvent", "audit");
            builder.HasKey(a => a.AuditEventId);
            builder.Property(a => a.AuditEventId).ValueGeneratedOnAdd();
            builder.Property(a => a.OccurredAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            builder.Property(a => a.ActionCode).IsRequired();
            builder.Property(a => a.EntityType).IsRequired();
            builder.Property(a => a.EntityId);
            builder.ToTable(t => t.HasCheckConstraint("CK_AuditEvent_DetailJson", "detail_json IS NULL OR ISJSON(detail_json) = 1"));
            builder.HasIndex(a => new { a.EntityType, a.EntityId, a.OccurredAtUtc }).HasDatabaseName("ix_audit_event_entity_time");
            builder.HasIndex(a => new { a.ActorUserId, a.OccurredAtUtc }).HasDatabaseName("ix_audit_event_actor_time");
            builder.HasOne(a => a.ActorUser).WithMany().HasForeignKey(a => a.ActorUserId);
        }
    }
}
