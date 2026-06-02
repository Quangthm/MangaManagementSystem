using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaManagementSystem.Infrastructure.Persistence.Configurations
{
    public class SeriesBoardPollConfiguration : IEntityTypeConfiguration<SeriesBoardPoll>
    {
        public void Configure(EntityTypeBuilder<SeriesBoardPoll> builder)
        {
            builder.ToTable("SeriesBoardPoll", "manga");
            builder.HasKey(p => p.SeriesBoardPollId);
            builder.Property(p => p.SeriesBoardPollId).ValueGeneratedOnAdd();
            builder.Property(p => p.PollTypeCode).IsRequired().HasMaxLength(50);
            builder.Property(p => p.PollReason).IsRequired();
            builder.Property(p => p.PollStatusCode).IsRequired().HasMaxLength(50).HasDefaultValue("OPEN");
            builder.Property(p => p.StartedAtUtc).IsRequired();
            builder.HasIndex(p => new { p.SeriesId, p.PollTypeCode })
                .IsUnique()
                .HasDatabaseName("ux_series_board_poll_open_type")
                .HasFilter("poll_status_code = N'OPEN'");
            builder.HasOne(p => p.Series).WithMany().HasForeignKey(p => p.SeriesId);
            builder.HasOne(p => p.CreatedByUser).WithMany().HasForeignKey(p => p.CreatedByUserId);
        }
    }
}
