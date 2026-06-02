using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaManagementSystem.Infrastructure.Persistence.Configurations
{
    public class SeriesRankingSnapshotConfiguration : IEntityTypeConfiguration<SeriesRankingSnapshot>
    {
        public void Configure(EntityTypeBuilder<SeriesRankingSnapshot> builder)
        {
            builder.ToTable("SeriesRankingSnapshot", "manga", t =>
            {
                t.HasCheckConstraint("CK_SeriesRankingSnapshot_RankingPeriodTypeCode", "ranking_period_type_code IN ('WEEKLY','MONTHLY','SEASONAL')");
                t.HasCheckConstraint("CK_SeriesRankingSnapshot_RankPosition", "rank_position >= 1");
            });
            builder.HasKey(s => s.SeriesRankingSnapshotId);
            builder.Property(s => s.SeriesRankingSnapshotId).ValueGeneratedOnAdd();
            builder.Property(s => s.RankingPeriodTypeCode).IsRequired().HasMaxLength(20);
            builder.Property(s => s.PeriodStartDate).IsRequired();
            builder.Property(s => s.PeriodEndDate).IsRequired();
            builder.Property(s => s.RankPosition).IsRequired();
            builder.Property(s => s.RankingScore).IsRequired().HasPrecision(10, 2);
            builder.Property(s => s.CancellationRiskScore).HasPrecision(10, 2);
            builder.HasIndex(s => new { s.SeriesId, s.RankingPeriodTypeCode, s.PeriodStartDate }).IsUnique();
            // moved into ToTable above
            builder.HasOne(s => s.Series).WithMany().HasForeignKey(s => s.SeriesId);
            builder.HasOne(s => s.GeneratedByUser).WithMany().HasForeignKey(s => s.GeneratedByUserId);
        }
    }
}
