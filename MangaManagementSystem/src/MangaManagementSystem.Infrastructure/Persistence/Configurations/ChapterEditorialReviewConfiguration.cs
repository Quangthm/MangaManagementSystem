using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaManagementSystem.Infrastructure.Persistence.Configurations
{
    public class ChapterEditorialReviewConfiguration : IEntityTypeConfiguration<ChapterEditorialReview>
    {
        public void Configure(EntityTypeBuilder<ChapterEditorialReview> builder)
        {
            builder.ToTable("ChapterEditorialReview", "manga");
            builder.HasKey(r => r.ChapterEditorialReviewId);
            builder.Property(r => r.ChapterEditorialReviewId).ValueGeneratedOnAdd();
            builder.Property(r => r.DecisionCode).IsRequired().HasMaxLength(30);
            builder.HasCheckConstraint("CK_ChapterEditorialReview_DecisionCode", "[DecisionCode] IN ('APPROVED','REVISION_REQUESTED','CANCELLED')");
            builder.HasOne(r => r.Chapter).WithMany().HasForeignKey(r => r.ChapterId);
            builder.HasOne(r => r.ReviewerUser).WithMany().HasForeignKey(r => r.ReviewerUserId);
            builder.HasOne(r => r.MarkupFile).WithMany().HasForeignKey(r => r.MarkupFileId);
        }
    }
}
