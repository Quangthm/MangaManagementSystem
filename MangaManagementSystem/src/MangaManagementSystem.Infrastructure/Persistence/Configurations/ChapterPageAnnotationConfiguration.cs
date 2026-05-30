using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaManagementSystem.Infrastructure.Persistence.Configurations
{
    public class ChapterPageAnnotationConfiguration : IEntityTypeConfiguration<ChapterPageAnnotation>
    {
        public void Configure(EntityTypeBuilder<ChapterPageAnnotation> builder)
        {
            builder.ToTable("ChapterPageAnnotation", "manga");
            builder.HasKey(a => a.ChapterPageAnnotationId);
            builder.Property(a => a.ChapterPageAnnotationId).ValueGeneratedOnAdd();
            builder.Property(a => a.IssueTypeCode).IsRequired().HasMaxLength(50);
            builder.HasIndex(a => a.PageRegionId);
            builder.HasCheckConstraint("CK_ChapterPageAnnotation_IssueTypeCode", "[IssueTypeCode] IN ('TRANSLATION_ERROR','ART_ERROR','LAYOUT_ERROR','OTHER')");
            builder.HasOne(a => a.PageRegion).WithMany().HasForeignKey(a => a.PageRegionId);
            builder.HasOne(a => a.AnnotatedByUser).WithMany().HasForeignKey(a => a.AnnotatedByUserId);
            builder.HasOne(a => a.ResolvedByUser).WithMany().HasForeignKey(a => a.ResolvedByUserId);
        }
    }
}
