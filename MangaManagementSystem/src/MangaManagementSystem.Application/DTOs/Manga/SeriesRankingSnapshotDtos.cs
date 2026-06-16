using System;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record SeriesRankingSnapshotDto(
        Guid SeriesRankingSnapshotId,
        Guid SeriesId,
        string RankingPeriodTypeCode,
        System.DateTime PeriodStartDate,
        System.DateTime PeriodEndDate,
        int RankPosition,
        decimal RankingScore,
        Guid? GeneratedByUserId
    );

    public record CreateSeriesRankingSnapshotDto(
        [Required] Guid SeriesId,
        [Required][MaxLength(50)] string RankingPeriodTypeCode,
        [Required] DateTime PeriodStartDate,
        [Required] DateTime PeriodEndDate,
        [Required] int RankPosition,
        [Required] decimal RankingScore,
        Guid? GeneratedByUserId
    );
}
