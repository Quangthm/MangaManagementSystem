namespace MangaManagementSystem.Domain.Entities;

public sealed class SeriesRankingViewRow
{
    public Guid PublicationPeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public string PeriodTypeCode { get; set; } = string.Empty;
    public DateTime PeriodStartDate { get; set; }
    public DateTime PeriodEndDate { get; set; }
    public Guid SeriesId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public int RatingCount { get; set; }
    public decimal AverageRating { get; set; }
    public int ReadingCount { get; set; }
    public decimal RankingScore { get; set; }
    public int RankPosition { get; set; }
}
