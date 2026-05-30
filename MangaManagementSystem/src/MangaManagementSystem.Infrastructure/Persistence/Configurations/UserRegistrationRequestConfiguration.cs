using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaManagementSystem.Infrastructure.Persistence.Configurations
{
    public class UserRegistrationRequestConfiguration : IEntityTypeConfiguration<UserRegistrationRequest>
    {
        public void Configure(EntityTypeBuilder<UserRegistrationRequest> builder)
        {
            builder.ToTable("UserRegistrationRequest", "auth");
            builder.HasKey(x => x.RegistrationRequestId);
            builder.Property(x => x.RegistrationRequestId).ValueGeneratedOnAdd();
            builder.Property(x => x.Status).HasMaxLength(30).HasDefaultValue("PENDING");
            builder.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            builder.HasCheckConstraint("CK_UserRegistrationRequest_Status", "[Status] IN ('PENDING','APPROVED','REJECTED','CANCELLED')");
            builder.HasIndex(x => new { x.Status, x.CreatedAt }).HasDatabaseName("ix_user_registration_status");
            builder.HasIndex(x => x.UserId).IsUnique().HasDatabaseName("ux_user_registration_request_pending").HasFilter("[Status] = 'PENDING'");

            builder.HasOne(x => x.User)
                .WithMany(u => u.RegistrationRequests)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.RequestedRole)
                .WithMany()
                .HasForeignKey(x => x.RequestedRoleId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.PortfolioFile)
                .WithMany()
                .HasForeignKey(x => x.PortfolioFileId);

            builder.HasOne(x => x.ReviewedByUser)
                .WithMany() // Leave empty to avoid ambiguity
                .HasForeignKey(x => x.ReviewedByUserId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}