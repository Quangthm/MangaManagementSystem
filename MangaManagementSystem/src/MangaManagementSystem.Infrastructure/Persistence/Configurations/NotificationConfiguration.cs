using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaManagementSystem.Infrastructure.Persistence.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("Notification", "manga");
            builder.HasKey(n => n.NotificationId);
            builder.Property(n => n.NotificationId).ValueGeneratedOnAdd();
            builder.Property(n => n.NotificationTypeCode).IsRequired().HasMaxLength(50).HasDefaultValue("SYSTEM_MESSAGE");
            builder.Property(n => n.Title).IsRequired().HasMaxLength(200);
            builder.Property(n => n.Message).IsRequired();
            builder.Property(n => n.CreatedAtUtc).IsRequired();
            builder.HasIndex(n => new { n.RecipientUserId, n.ReadAtUtc, n.CreatedAtUtc }).HasDatabaseName("ix_notification_recipient_read_created");
            builder.HasIndex(n => n.RecipientUserId).HasDatabaseName("ix_notification_unread_recipient").HasFilter("read_at_utc IS NULL");
            builder.HasIndex(n => new { n.RelatedEntityType, n.RelatedEntityId }).HasDatabaseName("ix_notification_related_entity").HasFilter("related_entity_type IS NOT NULL AND related_entity_id IS NOT NULL");
            builder.HasOne(n => n.RecipientUser).WithMany().HasForeignKey(n => n.RecipientUserId);
        }
    }
}
