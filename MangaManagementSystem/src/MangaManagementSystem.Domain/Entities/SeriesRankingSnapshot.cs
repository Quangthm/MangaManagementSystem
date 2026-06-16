using MangaManagementSystem.Domain.Common;
using System;

namespace MangaManagementSystem.Domain.Entities
{
    public class SeriesRankingSnapshot : BaseEntity
    {
        public Guid SeriesRankingSnapshotId { get; set; }
        public Guid SeriesId { get; set; }
        public Series? Series { get; set; }
        public string RankingPeriodTypeCode { get; set; } = null!;
        public DateTime PeriodStartDate { get; set; }
        public DateTime PeriodEndDate { get; set; }
        public int RankPosition { get; set; }
        public decimal RankingScore { get; set; }
        public decimal? CancellationRiskScore { get; set; }
        public Guid? GeneratedByUserId { get; set; }
        public User? GeneratedByUser { get; set; }
    }
}
