using System;

namespace MangaManagementSystem.Domain.Entities
{
    public class ActiveSeriesContributor
    {
        public Guid SeriesId { get; set; }
        public Guid UserId { get; set; }
        public string RoleName { get; set; } = null!;
        public string UserStatusCode { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
