using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaManagementSystem.Infrastructure.Persistence.Configurations
{
    public class SeriesRankingSnapshotConfiguration : IEntityTypeConfiguration<SeriesRankingSnapshot>
    {
        public void Configure(EntityTypeBuilder<SeriesRankingSnapshot> builder)
        {
            builder.ToTable("SeriesRankingSnapshot", "manga");
            builder.HasKey(s => s.SeriesRankingSnapshotId);
            builder.Property(s => s.SeriesRankingSnapshotId).ValueGeneratedOnAdd();
            builder.Property(s => s.RankingPeriodTypeCode).IsRequired().HasMaxLength(20);
            builder.Property(s => s.PeriodStartDate).IsRequired();
            builder.Property(s => s.PeriodEndDate).IsRequired();
            builder.Property(s => s.RankPosition).IsRequired();
            builder.Property(s => s.RankingScore).IsRequired().HasPrecision(18, 2);
            builder.HasIndex(s => new { s.SeriesId, s.RankingPeriodTypeCode, s.PeriodStartDate }).IsUnique();
            builder.HasCheckConstraint("CK_SeriesRankingSnapshot_RankingPeriodTypeCode", "[RankingPeriodTypeCode] IN ('WEEKLY','MONTHLY','SEASONAL')");
            builder.HasCheckConstraint("CK_SeriesRankingSnapshot_RankPosition", "[RankPosition] >= 1");
            builder.HasOne(s => s.Series).WithMany().HasForeignKey(s => s.SeriesId);
            builder.HasOne(s => s.GeneratedByUser).WithMany().HasForeignKey(s => s.GeneratedByUserId);
        }
    }
}
