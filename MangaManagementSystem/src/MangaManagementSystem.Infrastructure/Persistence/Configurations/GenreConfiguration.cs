using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaManagementSystem.Infrastructure.Persistence.Configurations
{
    public class GenreConfiguration : IEntityTypeConfiguration<Genre>
    {
        public void Configure(EntityTypeBuilder<Genre> builder)
        {
            builder.ToTable("Genre", "manga");
            builder.HasKey(g => g.GenreId);
            builder.Property(g => g.GenreId).ValueGeneratedOnAdd();
            builder.Property(g => g.GenreName).IsRequired().HasMaxLength(100);
            builder.Property(g => g.Description).HasMaxLength(500);
            builder.HasIndex(g => g.GenreName).IsUnique();
        }
    }
}
