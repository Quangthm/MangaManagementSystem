using MangaManagementSystem.Domain.Common;

namespace MangaManagementSystem.Domain.Entities
{
public class ChapterPageTaskRegion : BaseEntity
{
    public Guid ChapterPageTaskId { get; set; }
    public ChapterPageTask? ChapterPageTask { get; set; }
    public Guid PageRegionId { get; set; }
    public PageRegion? PageRegion { get; set; }
}
}
