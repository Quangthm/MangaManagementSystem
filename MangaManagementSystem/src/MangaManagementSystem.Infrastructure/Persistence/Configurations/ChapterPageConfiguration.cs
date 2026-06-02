using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaManagementSystem.Infrastructure.Persistence.Configurations
{
    public class ChapterPageConfiguration : IEntityTypeConfiguration<ChapterPage>
    {
        public void Configure(EntityTypeBuilder<ChapterPage> builder)
        {
            builder.ToTable("ChapterPage", "manga");
            builder.HasKey(cp => cp.ChapterPageId);
            builder.Property(cp => cp.ChapterPageId).ValueGeneratedOnAdd();
            builder.Property(cp => cp.PageNo).IsRequired();
            builder.HasIndex(cp => cp.ChapterId).HasDatabaseName("ix_chapter_page_chapter_id");
            builder.HasIndex(cp => new { cp.ChapterId, cp.PageNo }).IsUnique().HasDatabaseName("ux_chapter_page_active_page_no").HasFilter("deleted_at_utc IS NULL");
            builder.HasOne(cp => cp.Chapter).WithMany().HasForeignKey(cp => cp.ChapterId);
            builder.HasOne(cp => cp.DeletedByUser).WithMany().HasForeignKey(cp => cp.DeletedByUserId);
        }
    }
}
