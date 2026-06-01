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
            builder.Property(v => v.AverageRating).HasPrecision(4, 2);
            builder.Property(v => v.DataSourceNote).HasMaxLength(500);
            builder.Property(v => v.EnteredByUserId).IsRequired();
            builder.Property(v => v.VotedAtUtc).IsRequired();
            builder.HasIndex(v => v.ChapterId).IsUnique();
            builder.HasOne(v => v.Chapter).WithMany().HasForeignKey(v => v.ChapterId);
            builder.HasOne(v => v.EnteredByUser).WithMany().HasForeignKey(v => v.EnteredByUserId);
        }
    }
}
