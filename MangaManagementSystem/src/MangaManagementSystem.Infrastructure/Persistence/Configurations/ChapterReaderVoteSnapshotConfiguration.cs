using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaManagementSystem.Infrastructure.Persistence.Configurations
{
    public class ChapterReaderVoteSnapshotConfiguration : IEntityTypeConfiguration<ChapterReaderVoteSnapshot>
    {
        public void Configure(EntityTypeBuilder<ChapterReaderVoteSnapshot> builder)
        {
            builder.ToTable("ChapterReaderVoteSnapshot", "manga");
            builder.HasKey(v => v.ChapterReaderVoteSnapshotId);
            builder.Property(v => v.ChapterReaderVoteSnapshotId).ValueGeneratedOnAdd();
            builder.Property(v => v.ReaderVoteCount).IsRequired();
            builder.Property(v => v.AverageRating).IsRequired().HasPrecision(10, 2);
            builder.HasIndex(v => v.ChapterId).IsUnique();
            builder.HasCheckConstraint("CK_ChapterReaderVoteSnapshot_AverageRating", "[AverageRating] BETWEEN 0 AND 10");
            builder.HasOne(v => v.Chapter).WithMany().HasForeignKey(v => v.ChapterId);
            builder.HasOne(v => v.EnteredByUser).WithMany().HasForeignKey(v => v.EnteredByUserId);
        }
    }
}
