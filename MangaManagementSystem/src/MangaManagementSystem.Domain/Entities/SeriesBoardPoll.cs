using MangaManagementSystem.Domain.Common;
using System;

namespace MangaManagementSystem.Domain.Entities
{
    public class SeriesBoardPoll : BaseEntity
    {
        public long SeriesBoardPollId { get; set; }
        public long SeriesId { get; set; }
        public Series? Series { get; set; }
        public string PollTypeCode { get; set; } = null!;
        public string PollStatusCode { get; set; } = "OPEN";
        public DateTime CreatedAtUtc { get; set; }
    }
}
