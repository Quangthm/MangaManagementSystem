using MangaManagementSystem.Domain.Common;

namespace MangaManagementSystem.Domain.Entities
{
    public class ChapterPageTaskRegion : BaseEntity
    {
        public long ChapterPageTaskRegionId { get; set; }
        public long ChapterPageTaskId { get; set; }
        public ChapterPageTask? ChapterPageTask { get; set; }
        public long PageRegionId { get; set; }
        public PageRegion? PageRegion { get; set; }
    }
}
