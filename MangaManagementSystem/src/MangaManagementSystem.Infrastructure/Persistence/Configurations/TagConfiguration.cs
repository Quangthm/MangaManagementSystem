using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaManagementSystem.Infrastructure.Persistence.Configurations
{
    public class TagConfiguration : IEntityTypeConfiguration<Tag>
    {
        public void Configure(EntityTypeBuilder<Tag> builder)
        {
            builder.ToTable("Tag", "manga");
            builder.HasKey(t => t.TagId);
            builder.Property(t => t.TagId).ValueGeneratedOnAdd();
            builder.Property(t => t.TagName).IsRequired().HasMaxLength(100);
            builder.Property(t => t.Description).HasMaxLength(500);
            builder.HasIndex(t => t.TagName).IsUnique();
        }
    }
}
