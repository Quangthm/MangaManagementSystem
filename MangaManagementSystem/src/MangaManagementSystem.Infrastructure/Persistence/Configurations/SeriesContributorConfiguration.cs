using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaManagementSystem.Infrastructure.Persistence.Configurations
{
    public class SeriesContributorConfiguration : IEntityTypeConfiguration<SeriesContributor>
    {
        public void Configure(EntityTypeBuilder<SeriesContributor> builder)
        {
            builder.ToTable("series_contributor", "manga", t =>
            {
                t.HasCheckConstraint(
                    "CK_SeriesContributor_EndDate",
                    "end_date IS NULL OR end_date >= start_date"
                );
            });

            builder.HasKey(sc => sc.SeriesContributorId);

            builder.Property(sc => sc.SeriesContributorId)
                .ValueGeneratedOnAdd();

            builder.Property(sc => sc.StartDate)
                .IsRequired();

            builder.Property(sc => sc.Notes)
                .HasMaxLength(500);

            // Lưu lịch sử: cùng 1 user có thể từng tham gia nhiều giai đoạn khác nhau,
            // nhưng không được trùng SeriesId + UserId + StartDate.
            builder.HasIndex(sc => new { sc.SeriesId, sc.UserId, sc.StartDate })
                .IsUnique();

            // FR-SC-005 + FR-SC-006:
            // Không cho 1 user active trong cùng 1 series nhiều hơn 1 lần.
            // Active contributor = EndDate IS NULL.
            builder.HasIndex(sc => new { sc.SeriesId, sc.UserId })
                .HasDatabaseName("ux_series_contributor_active")
                .HasFilter("end_date IS NULL")
                .IsUnique();

            // Index hỗ trợ query danh sách contributor active theo series.
            builder.HasIndex(sc => sc.SeriesId)
                .HasDatabaseName("ix_series_contributor_series_active")
                .HasFilter("end_date IS NULL");

            // Index hỗ trợ query danh sách series mà user đang tham gia.
            builder.HasIndex(sc => sc.UserId)
                .HasDatabaseName("ix_series_contributor_user_active")
                .HasFilter("end_date IS NULL");

            builder.HasOne(sc => sc.Series)
                .WithMany()
                .HasForeignKey(sc => sc.SeriesId);

            builder.HasOne(sc => sc.User)
                .WithMany()
                .HasForeignKey(sc => sc.UserId);
        }
    }
}