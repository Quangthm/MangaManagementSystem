using System;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record SeriesRankingSnapshotDto(
        long SeriesRankingSnapshotId,
        long SeriesId,
        string RankingPeriodTypeCode,
        System.DateTime PeriodStartDate,
        System.DateTime PeriodEndDate,
        int RankPosition,
        decimal RankingScore,
        int? GeneratedByUserId
    );

    public record CreateSeriesRankingSnapshotDto(
        [Required] long SeriesId,
        [Required][MaxLength(50)] string RankingPeriodTypeCode,
        [Required] DateTime PeriodStartDate,
        [Required] DateTime PeriodEndDate,
        [Required] int RankPosition,
        [Required] decimal RankingScore,
        int? GeneratedByUserId
    );
}
