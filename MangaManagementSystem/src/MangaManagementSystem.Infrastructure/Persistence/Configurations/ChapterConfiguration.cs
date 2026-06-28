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
                t.HasCheckConstraint("CK_Chapter_StatusCode", "status_code IN ('DRAFT','UNDER_REVIEW','REVISION_REQUESTED','APPROVED','SCHEDULED','RELEASED','ON_HOLD','CANCELLED')");
            });
            builder.HasKey(c => c.ChapterId);
            builder.Property(c => c.ChapterId).ValueGeneratedOnAdd();
            builder.Property(c => c.ChapterNumberLabel).IsRequired().HasMaxLength(20);
            builder.Property(c => c.StatusCode).HasMaxLength(50).HasDefaultValue("DRAFT");
            builder.Property(c => c.PlannedReleaseDate).HasColumnType("date");
            builder.Property(c => c.CreatedAtUtc).IsRequired();
            builder.Property(c => c.UpdatedAtUtc);
            builder.HasIndex(c => c.SeriesId).HasDatabaseName("ix_chapter_series_id");
            builder.HasIndex(c => c.StatusCode).HasDatabaseName("ix_chapter_status_code");
            builder.HasIndex(c => new { c.SeriesId, c.ChapterNumberLabel }).IsUnique();
            // moved into ToTable above
            builder.HasOne(c => c.Series).WithMany(s => s.Chapters).HasForeignKey(c => c.SeriesId);
            builder.HasOne(c => c.CreatedByUser).WithMany().HasForeignKey(c => c.CreatedByUserId);
        }
    }
}
