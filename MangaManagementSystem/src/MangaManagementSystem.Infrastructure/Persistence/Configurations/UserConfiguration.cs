using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaManagementSystem.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users", "auth");
            builder.HasKey(u => u.UserId);
            builder.Property(u => u.UserId).ValueGeneratedOnAdd();
            builder.Property(u => u.Username).IsRequired().HasMaxLength(50);
            builder.Property(u => u.Email).IsRequired().HasMaxLength(254);
            builder.Property(u => u.DisplayName).IsRequired().HasMaxLength(100);
            builder.Property(u => u.PasswordHash).IsRequired().HasMaxLength(255);
            builder.Property(u => u.StatusCode).IsRequired().HasMaxLength(30).HasDefaultValue("PENDING_APPROVAL");
            builder.Property(u => u.CreatedAtUtc).IsRequired();
            builder.HasIndex(u => u.Username).IsUnique();
            builder.HasIndex(u => u.Email).IsUnique();
            builder.HasIndex(u => new { u.StatusCode, u.CreatedAtUtc }).HasDatabaseName("ix_users_status_created");
            builder.HasIndex(u => new { u.RoleId, u.StatusCode }).HasDatabaseName("ix_users_role_status");
            builder.HasOne(u => u.Role).WithMany(r => r.Users).HasForeignKey(u => u.RoleId);
            builder.HasOne(u => u.AvatarFile).WithMany().HasForeignKey(u => u.AvatarFileId);
            builder.HasOne(u => u.PortfolioFile).WithMany().HasForeignKey(u => u.PortfolioFileId);
        }
    }
}
