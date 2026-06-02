using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaManagementSystem.Infrastructure.Persistence.Configurations
{
    public class ChapterPageTaskRegionConfiguration : IEntityTypeConfiguration<ChapterPageTaskRegion>
    {
        public void Configure(EntityTypeBuilder<ChapterPageTaskRegion> builder)
        {
            builder.ToTable("ChapterPageTaskRegion", "manga");
            builder.HasKey(tr => new { tr.ChapterPageTaskId, tr.PageRegionId });
            builder.HasOne(tr => tr.ChapterPageTask).WithMany().HasForeignKey(tr => tr.ChapterPageTaskId);
            builder.HasOne(tr => tr.PageRegion).WithMany().HasForeignKey(tr => tr.PageRegionId);
        }
    }
}
