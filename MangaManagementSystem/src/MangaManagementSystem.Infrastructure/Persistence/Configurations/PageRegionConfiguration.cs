using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaManagementSystem.Infrastructure.Persistence.Configurations
{
    public class PageRegionConfiguration : IEntityTypeConfiguration<PageRegion>
    {
        public void Configure(EntityTypeBuilder<PageRegion> builder)
        {
            builder.ToTable("PageRegion", "manga", t =>
            {
                t.HasCheckConstraint("CK_PageRegion_TypeCode", "type_code IN ('PANEL','SPEECH_BUBBLE','CHARACTER','SFX_TEXT','BACKGROUND','FULL_PAGE','OTHER')");
                t.HasCheckConstraint("CK_PageRegion_SourceType", "source_type IN ('AI','MANUAL')");
                t.HasCheckConstraint("CK_PageRegion_Coordinates_NonNegative", "x >= 0 AND y >= 0");
                t.HasCheckConstraint("CK_PageRegion_Dimensions", "(width = 0 AND height = 0) OR (width > 0 AND height > 0)");
                t.HasCheckConstraint("CK_PageRegion_FullPageShape", "type_code <> N'FULL_PAGE' OR (x = 0 AND y = 0 AND width > 0 AND height > 0)");
            });
            builder.HasKey(r => r.PageRegionId);
            builder.Property(r => r.PageRegionId).ValueGeneratedOnAdd();
            builder.Property(r => r.TypeCode).IsRequired().HasMaxLength(80).HasDefaultValue("OTHER");
            builder.Property(r => r.SourceType).IsRequired().HasMaxLength(20).HasDefaultValue("MANUAL");
            builder.Property(r => r.X).IsRequired().HasPrecision(10, 2);
            builder.Property(r => r.Y).IsRequired().HasPrecision(10, 2);
            builder.Property(r => r.Width).IsRequired().HasPrecision(10, 2).HasDefaultValue(0m);
            builder.Property(r => r.Height).IsRequired().HasPrecision(10, 2).HasDefaultValue(0m);
            builder.Property(x => x.ConfidenceScore).HasPrecision(5, 4);
            builder.HasIndex(r => new { r.ChapterPageVersionId, r.TypeCode }).HasDatabaseName("ix_page_region_version_type");
            builder.HasIndex(r => r.TypeCode).HasDatabaseName("ix_page_region_type");
            builder.HasOne(r => r.ChapterPageVersion)
                .WithMany()
                .HasForeignKey(r => r.ChapterPageVersionId);
            builder.HasOne(r => r.CreatedByUser)
                .WithMany()
                .HasForeignKey(r => r.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
