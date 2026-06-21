using System;
using System.Collections.Generic;

namespace MangaManagementSystem.Domain.Entities
{
    public class Tag
    {
        public Guid TagId { get; set; }
        public string TagName { get; set; } = null!;
        public string? Description { get; set; }
        public ICollection<Series> Series { get; set; } = new List<Series>();
    }
}
