using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaManagementSystem.Infrastructure.Persistence.Configurations
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("Roles", "auth");
            builder.HasKey(r => r.RoleId);
            builder.Property(r => r.RoleId).ValueGeneratedOnAdd();
            builder.Property(r => r.RoleName).IsRequired().HasMaxLength(30);
            builder.HasIndex(r => r.RoleName).IsUnique();
        }
    }
}
