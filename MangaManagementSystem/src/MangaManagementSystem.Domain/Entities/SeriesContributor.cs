using MangaManagementSystem.Domain.Common;
using System;

namespace MangaManagementSystem.Domain.Entities
{
    public class SeriesContributor : BaseEntity
    {
        public Guid SeriesContributorId { get; set; }
        public Guid SeriesId { get; set; }
        public Guid UserId { get; set; }
        public User? User { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Notes { get; set; }
    }
}
