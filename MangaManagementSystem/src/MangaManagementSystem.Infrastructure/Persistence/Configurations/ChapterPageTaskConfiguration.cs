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
            builder.Property(t => t.StatusCode).IsRequired().HasMaxLength(30).HasDefaultValue("ASSIGNED");
            builder.Property(t => t.PriorityLevel).IsRequired();
            builder.HasCheckConstraint("CK_ChapterPageTask_StatusCode", "[StatusCode] IN ('ASSIGNED','IN_PROGRESS','COMPLETED','CANCELLED')");
            builder.HasCheckConstraint("CK_ChapterPageTask_PriorityLevel", "[PriorityLevel] BETWEEN 1 AND 5");
            builder.HasOne(t => t.AssignedToUser).WithMany().HasForeignKey(t => t.AssignedToUserId);
            builder.HasOne(t => t.CompletedPageVersion).WithMany().HasForeignKey(t => t.CompletedPageVersionId);
        }
    }
}
