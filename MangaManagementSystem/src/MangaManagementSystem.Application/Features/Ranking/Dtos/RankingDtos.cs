namespace MangaManagementSystem.Application.Features.Ranking.Dtos;

public sealed record PublicationPeriodDto(
    Guid PublicationPeriodId,
    string PeriodName,
    string PeriodTypeCode,
    DateTime PeriodStartDate,
    DateTime PeriodEndDate);

public sealed record SeriesVoteInputDto(
    Guid SeriesVoteInputId,
    Guid PublicationPeriodId,
    Guid SeriesId,
    string SeriesTitle,
    string? SeriesSlug,
    string? CoverUrl,
    int RatingCount,
    decimal AverageRating,
    int ReadingCount,
    string? DataSourceNote,
    Guid EnteredByUserId,
    DateTime EnteredAtUtc,
    Guid? UpdatedByUserId,
    DateTime? UpdatedAtUtc);

public sealed record SeriesRankingRowDto(
    Guid PublicationPeriodId,
    string PeriodName,
    string PeriodTypeCode,
    DateTime PeriodStartDate,
    DateTime PeriodEndDate,
    Guid SeriesId,
    string SeriesTitle,
    string? SeriesSlug,
    string? CoverUrl,
    int RatingCount,
    decimal AverageRating,
    int ReadingCount,
    decimal RankingScore,
    int RankPosition);

public sealed record RankableSeriesDto(
    Guid SeriesId,
    string Title,
    string? Slug,
    string? CoverUrl);
