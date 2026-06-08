using MangaManagementSystem.Domain.Common;
using System;

namespace MangaManagementSystem.Domain.Entities
{
public class ChapterPage : BaseEntity
{
    public Guid ChapterPageId { get; set; }
    public Guid ChapterId { get; set; }
    public Chapter? Chapter { get; set; }
    public int PageNo { get; set; }
    public string? PageNotes { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public User? DeletedByUser { get; set; }
    }
}
