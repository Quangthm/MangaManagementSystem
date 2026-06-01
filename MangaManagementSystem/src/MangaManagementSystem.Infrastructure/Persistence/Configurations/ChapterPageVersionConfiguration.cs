using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaManagementSystem.Infrastructure.Persistence.Configurations
{
    public class ChapterPageVersionConfiguration : IEntityTypeConfiguration<ChapterPageVersion>
    {
        public void Configure(EntityTypeBuilder<ChapterPageVersion> builder)
        {
            builder.ToTable("ChapterPageVersion", "manga");
            builder.HasKey(v => v.ChapterPageVersionId);
            builder.Property(v => v.ChapterPageVersionId).ValueGeneratedOnAdd();
            builder.Property(v => v.VersionNo).IsRequired();
            builder.Property(v => v.IsCurrentVersion).HasDefaultValue(false);
            builder.HasIndex(v => new { v.ChapterPageId, v.VersionNo }).IsUnique();
            builder.HasIndex(v => v.ChapterPageId).HasDatabaseName("ix_chapter_page_chapter_id");
            builder.HasIndex(v => v.IsCurrentVersion).HasDatabaseName("ux_chapter_page_version_current").IsUnique().HasFilter("is_current_version = 1");
            builder.HasOne(v => v.ChapterPage).WithMany().HasForeignKey(v => v.ChapterPageId);
            builder.HasOne(v => v.PageFile).WithMany().HasForeignKey(v => v.PageFileId);
        }
    }
}
