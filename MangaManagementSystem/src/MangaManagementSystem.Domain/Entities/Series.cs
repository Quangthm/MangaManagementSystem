using MangaManagementSystem.Domain.Common;
using System;
using System.Collections.Generic;

namespace MangaManagementSystem.Domain.Entities
{
public class Series : BaseEntity
{
    public Guid SeriesId { get; set; }
    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string Synopsis { get; set; } = null!;
    public Guid? CoverFileId { get; set; }
    public FileResource? CoverFile { get; set; }
    public string StatusCode { get; set; } = "PROPOSAL_DRAFT";
    public string ContentLanguageCode { get; set; } = "ja";
    public Guid? SourceSeriesId { get; set; }
    public Series? SourceSeries { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public User? UpdatedByUser { get; set; }
    public string? PublicationFrequencyCode { get; set; }
    public ICollection<Genre> Genres { get; set; } = new List<Genre>();
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    public virtual ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();
}
}
