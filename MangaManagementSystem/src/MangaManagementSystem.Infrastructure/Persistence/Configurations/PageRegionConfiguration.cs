using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaManagementSystem.Infrastructure.Persistence.Configurations
{
    public class PageRegionConfiguration : IEntityTypeConfiguration<PageRegion>
    {
        public void Configure(EntityTypeBuilder<PageRegion> builder)
        {
            builder.ToTable("PageRegion", "manga");
            builder.HasKey(r => r.PageRegionId);
            builder.Property(r => r.PageRegionId).ValueGeneratedOnAdd();
            builder.Property(r => r.TypeCode).IsRequired().HasMaxLength(80).HasDefaultValue("OTHER");
            builder.Property(r => r.SourceType).IsRequired().HasMaxLength(20).HasDefaultValue("MANUAL");
            builder.Property(r => r.X).IsRequired().HasPrecision(18, 2);
            builder.Property(r => r.Y).IsRequired().HasPrecision(18, 2);
            builder.Property(r => r.Width).IsRequired().HasPrecision(18, 2);
            builder.Property(r => r.Height).IsRequired().HasPrecision(18, 2);
            builder.Property(x => x.ConfidenceScore).HasPrecision(18, 2);
            builder.HasIndex(r => new { r.ChapterPageVersionId, r.TypeCode }).HasDatabaseName("ix_page_region_version_type");
            builder.HasIndex(r => r.TypeCode).HasDatabaseName("ix_page_region_type");
            builder.HasCheckConstraint("CK_PageRegion_TypeCode", "[TypeCode] IN ('PANEL','SPEECH_BUBBLE','CHARACTER','SFX_TEXT','BACKGROUND','OTHER')");
            builder.HasCheckConstraint("CK_PageRegion_SourceType", "[SourceType] IN ('AI','MANUAL')");
            builder.HasCheckConstraint("CK_PageRegion_Width_Height", "[Width] > 0 AND [Height] > 0");
            builder.HasOne(r => r.ChapterPageVersion).WithMany().HasForeignKey(r => r.ChapterPageVersionId);
            builder.HasOne(r => r.CreatedByUser).WithMany().HasForeignKey(r => r.CreatedByUserId);
            builder.HasOne(r => r.UpdatedByUser).WithMany().HasForeignKey(r => r.UpdatedByUserId);
        }
    }
}
