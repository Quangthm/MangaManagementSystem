using System;
using System.Collections.Generic;

namespace MangaManagementSystem.Domain.Entities
{
    /// <summary>
    /// Flat projection of a completed task with its page-region context.
    /// No EF navigation — populated by the repository via a single projection query.
    /// </summary>
    public sealed class AssistantCompletedTaskRow
    {
        public Guid ChapterPageTaskId { get; set; }
        public string TypeCode { get; set; } = string.Empty;
        public string StatusCode { get; set; } = string.Empty;
        public DateTime DueAtUtc { get; set; }
        public decimal? CompensationAmount { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public int RegionCount { get; set; }
        public string SeriesTitle { get; set; } = string.Empty;
        public string ChapterTitle { get; set; } = string.Empty;
        public int PageNumber { get; set; }
    }
}
