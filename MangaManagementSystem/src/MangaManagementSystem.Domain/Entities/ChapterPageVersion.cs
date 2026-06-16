using MangaManagementSystem.Domain.Common;
using System;

namespace MangaManagementSystem.Domain.Entities
{
public class ChapterPageVersion : BaseEntity
{
    public Guid ChapterPageVersionId { get; set; }
    public Guid ChapterPageId { get; set; }
    public ChapterPage? ChapterPage { get; set; }
    public short VersionNo { get; set; }
    public Guid PageFileId { get; set; }
        public FileResource? PageFile { get; set; }
        public string? VersionNote { get; set; }
        public bool IsCurrentVersion { get; set; }
    }
}
