using System;
using MangaManagementSystem.Domain.Common;

namespace MangaManagementSystem.Domain.Entities
{
    public class PublicationPeriod : BaseEntity
    {
        public Guid PublicationPeriodId { get; set; }
        public string PeriodName { get; set; } = null!;
        public string PeriodTypeCode { get; set; } = null!;
        public DateTime PeriodStartDate { get; set; }
        public DateTime PeriodEndDate { get; set; }
    }
}
