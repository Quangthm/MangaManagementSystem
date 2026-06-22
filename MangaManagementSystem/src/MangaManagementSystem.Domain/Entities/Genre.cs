using System;
using System.Collections.Generic;

namespace MangaManagementSystem.Domain.Entities
{
    public class Genre
    {
        public Guid GenreId { get; set; }
        public string GenreName { get; set; } = null!;
        public string? Description { get; set; }
        public ICollection<Series> Series { get; set; } = new List<Series>();
    }
}
