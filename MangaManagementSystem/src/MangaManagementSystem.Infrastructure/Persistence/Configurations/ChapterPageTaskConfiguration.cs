using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaManagementSystem.Infrastructure.Persistence.Configurations
{
    public class ChapterPageTaskConfiguration : IEntityTypeConfiguration<ChapterPageTask>
    {
        public void Configure(EntityTypeBuilder<ChapterPageTask> builder)
        {
            builder.ToTable("ChapterPageTask", "manga");
            builder.HasKey(t => t.ChapterPageTaskId);
            builder.Property(t => t.ChapterPageTaskId).ValueGeneratedOnAdd();
            builder.Property(t => t.TypeCode).IsRequired().HasMaxLength(50);
            builder.Property(t => t.StatusCode).IsRequired().HasMaxLength(50).HasDefaultValue("ASSIGNED");
            builder.Property(t => t.TaskTitle).IsRequired().HasMaxLength(200);
            builder.Property(t => t.TaskDescription).IsRequired();
            builder.Property(t => t.PriorityLevel).IsRequired().HasDefaultValue((byte)3);
            builder.Property(t => t.DueAtUtc).IsRequired();
            builder.Property(t => t.CompensationAmount).HasPrecision(12, 2);
            builder.Property(t => t.CreatedAtUtc).IsRequired();
            builder.HasOne(t => t.AssignedToUser).WithMany().HasForeignKey(t => t.AssignedToUserId);
            builder.HasOne(t => t.CreatedByUser).WithMany().HasForeignKey(t => t.CreatedByUserId);
            builder.HasOne(t => t.CompletedPageVersion).WithMany().HasForeignKey(t => t.CompletedPageVersionId);

            // Many-to-many through the existing manga.ChapterPageTaskRegion junction table.
            builder.HasMany(t => t.PageRegions)
                .WithMany(r => r.Tasks)
                .UsingEntity<Dictionary<string, object>>(
                    "ChapterPageTaskRegion",
                    right => right
                        .HasOne<PageRegion>()
                        .WithMany()
                        .HasForeignKey("PageRegionId")
                        .HasConstraintName("fk_chapter_page_task_region_region"),
                    left => left
                        .HasOne<ChapterPageTask>()
                        .WithMany()
                        .HasForeignKey("ChapterPageTaskId")
                        .HasConstraintName("fk_chapter_page_task_region_task"),
                    join =>
                    {
                        join.ToTable("ChapterPageTaskRegion", "manga");

                        join.HasKey("ChapterPageTaskId", "PageRegionId")
                            .HasName("pk_chapter_page_task_region");

                        join.Property<Guid>("ChapterPageTaskId")
                            .HasColumnName("chapter_page_task_id");

                        join.Property<Guid>("PageRegionId")
                            .HasColumnName("page_region_id");
                    });
        }
    }
}

