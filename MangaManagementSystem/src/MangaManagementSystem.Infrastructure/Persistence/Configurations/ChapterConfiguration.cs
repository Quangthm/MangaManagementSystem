using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaManagementSystem.Infrastructure.Persistence.Configurations
{
    public class ChapterConfiguration : IEntityTypeConfiguration<Chapter>
    {
        public void Configure(EntityTypeBuilder<Chapter> builder)
        {
            builder.ToTable("Chapter", "manga", t =>
            {
                t.HasCheckConstraint(
                    "CK_Chapter_StatusCode",
                    "status_code IN ('DRAFT','UNDER_REVIEW','REVISION_REQUESTED','APPROVED','SCHEDULED','RELEASED','ON_HOLD','CANCELLED')"
                );

                t.HasCheckConstraint(
                    "CK_Chapter_ScheduledRequiresPlannedDate",
                    "status_code <> 'SCHEDULED' OR planned_release_date IS NOT NULL"
                );

                t.HasCheckConstraint(
                    "CK_Chapter_ReleasedRequiresReleasedAt",
                    "status_code <> 'RELEASED' OR released_at_utc IS NOT NULL"
                );

                t.HasCheckConstraint(
                    "CK_Chapter_ReleasedAtOnlyWhenReleased",
                    "released_at_utc IS NULL OR status_code = 'RELEASED'"
                );
            });

            builder.HasKey(c => c.ChapterId);

            builder.Property(c => c.ChapterId)
                .HasColumnName("chapter_id")
                .ValueGeneratedOnAdd();

            builder.Property(c => c.SeriesId)
                .HasColumnName("series_id")
                .IsRequired();

            builder.Property(c => c.ChapterNumberLabel)
                .HasColumnName("chapter_number_label")
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(c => c.ChapterTitle)
                .HasColumnName("chapter_title")
                .HasMaxLength(200);

            builder.Property(c => c.StatusCode)
                .HasColumnName("status_code")
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("DRAFT");

            builder.Property(c => c.PlannedReleaseDate)
                .HasColumnName("planned_release_date");

            builder.Property(c => c.ReleasedAtUtc)
                .HasColumnName("released_at_utc");

            builder.Property(c => c.CreatedAtUtc)
                .HasColumnName("created_at_utc")
                .IsRequired();

            builder.Property(c => c.CreatedByUserId)
                .HasColumnName("created_by_user_id");

            builder.Property(c => c.UpdatedAtUtc)
                .HasColumnName("updated_at_utc");

            builder.HasIndex(c => c.SeriesId)
                .HasDatabaseName("ix_chapter_series_id");

            builder.HasIndex(c => c.StatusCode)
                .HasDatabaseName("ix_chapter_status_code");

            builder.HasIndex(c => new { c.SeriesId, c.ChapterNumberLabel })
                .IsUnique();

            // Avoid EF Core creating shadow columns like series_id1 / created_by_user_id1.
            builder.Ignore(c => c.Series);
            builder.Ignore(c => c.CreatedByUser);
        }
    }
}