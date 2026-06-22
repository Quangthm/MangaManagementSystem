using System;
using System.Collections.Generic;
using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaManagementSystem.Infrastructure.Persistence.Configurations
{
    public class ChapterPageAnnotationConfiguration : IEntityTypeConfiguration<ChapterPageAnnotation>
    {
        public void Configure(EntityTypeBuilder<ChapterPageAnnotation> builder)
        {
            builder.ToTable("ChapterPageAnnotation", "manga", t =>
            {
                t.HasCheckConstraint("CK_ChapterPageAnnotation_IssueTypeCode", "(issue_type_code IS NULL) OR (issue_type_code IN ('BACKGROUND_INCONSISTENCY','SHADING_ERROR','EFFECTS_ERROR','CLEANUP_REQUIRED','DIALOGUE_ERROR','TYPESETTING_ERROR','TRANSLATION_ERROR','PANEL_ORDER_ERROR','CHARACTER_ANATOMY_ERROR','CONTINUITY_ERROR','OTHER'))");
            });
            builder.HasKey(a => a.ChapterPageAnnotationId);
            builder.Property(a => a.ChapterPageAnnotationId).ValueGeneratedOnAdd();
            builder.Property(a => a.IssueTypeCode).IsRequired().HasMaxLength(80);
            builder.HasOne(a => a.AnnotatedByUser).WithMany().HasForeignKey(a => a.AnnotatedByUserId);
            builder.HasOne(a => a.ResolvedByUser).WithMany().HasForeignKey(a => a.ResolvedByUserId);

            // Many-to-many through the existing manga.ChapterPageAnnotationRegion junction table.
            builder.HasMany(a => a.PageRegions)
                .WithMany(r => r.Annotations)
                .UsingEntity<Dictionary<string, object>>(
                    "ChapterPageAnnotationRegion",
                    right => right
                        .HasOne<PageRegion>()
                        .WithMany()
                        .HasForeignKey("PageRegionId")
                        .HasConstraintName("fk_chapter_page_annotation_region_region"),
                    left => left
                        .HasOne<ChapterPageAnnotation>()
                        .WithMany()
                        .HasForeignKey("ChapterPageAnnotationId")
                        .HasConstraintName("fk_chapter_page_annotation_region_annotation"),
                    join =>
                    {
                        join.ToTable("ChapterPageAnnotationRegion", "manga");

                        join.HasKey("ChapterPageAnnotationId", "PageRegionId")
                            .HasName("pk_chapter_page_annotation_region");

                        join.Property<Guid>("ChapterPageAnnotationId")
                            .HasColumnName("chapter_page_annotation_id");

                        join.Property<Guid>("PageRegionId")
                            .HasColumnName("page_region_id");
                    });
        }
    }
}
