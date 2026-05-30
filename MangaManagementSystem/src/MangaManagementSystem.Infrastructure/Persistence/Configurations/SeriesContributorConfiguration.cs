using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaManagementSystem.Infrastructure.Persistence.Configurations
{
    public class SeriesContributorConfiguration : IEntityTypeConfiguration<SeriesContributor>
    {
        public void Configure(EntityTypeBuilder<SeriesContributor> builder)
        {
            builder.ToTable("SeriesContributor", "manga");
            builder.HasKey(sc => sc.SeriesContributorId);
            builder.Property(sc => sc.SeriesContributorId).ValueGeneratedOnAdd();
            builder.Property(sc => sc.StartDate).IsRequired();
            builder.HasIndex(sc => new { sc.SeriesId, sc.UserId, sc.StartDate }).IsUnique();
            builder.HasIndex(sc => new { sc.SeriesId, sc.UserId }).IsUnique().HasDatabaseName("ux_series_contributor_active_role").HasFilter("[EndDate] IS NULL");
            builder.HasIndex(sc => sc.SeriesId).HasDatabaseName("ix_series_contributor_series_active").HasFilter("[EndDate] IS NULL");
            builder.HasIndex(sc => sc.UserId).HasDatabaseName("ix_series_contributor_user_active").HasFilter("[EndDate] IS NULL");
            builder.HasCheckConstraint("CK_SeriesContributor_EndDate", "[EndDate] IS NULL OR [EndDate] >= [StartDate]");
            builder.HasOne(sc => sc.Series).WithMany().HasForeignKey(sc => sc.SeriesId);
            builder.HasOne(sc => sc.User).WithMany().HasForeignKey(sc => sc.UserId);
        }
    }
}
