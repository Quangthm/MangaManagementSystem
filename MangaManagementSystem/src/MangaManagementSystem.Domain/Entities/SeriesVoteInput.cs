using MangaManagementSystem.Domain.Common;

namespace MangaManagementSystem.Domain.Entities;

public sealed class SeriesVoteInput : BaseEntity
{
    public Guid SeriesVoteInputId { get; set; }
    public Guid PublicationPeriodId { get; set; }
    public PublicationPeriod PublicationPeriod { get; set; } = null!;
    public Guid SeriesId { get; set; }
    public Series Series { get; set; } = null!;
    public int RatingCount { get; set; }
    public decimal AverageRating { get; set; }
    public int ReadingCount { get; set; }
    public string? DataSourceNote { get; set; }
    public Guid EnteredByUserId { get; set; }
    public User EnteredByUser { get; set; } = null!;
    public DateTime EnteredAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public User? UpdatedByUser { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
