using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaManagementSystem.Infrastructure.Persistence.Configurations;

public sealed class SeriesVoteInputConfiguration : IEntityTypeConfiguration<SeriesVoteInput>
{
    public void Configure(EntityTypeBuilder<SeriesVoteInput> builder)
    {
        builder.ToTable("SeriesVoteInput", "manga");

        builder.HasKey(v => v.SeriesVoteInputId);

        builder.Property(v => v.SeriesVoteInputId)
            .ValueGeneratedOnAdd();

        builder.Property(v => v.AverageRating)
            .HasColumnType("decimal(4,2)");

        builder.Property(v => v.DataSourceNote)
            .HasMaxLength(1000);

        builder.Property(v => v.EnteredAtUtc)
            .IsRequired();

        builder.HasIndex(v => new { v.PublicationPeriodId, v.SeriesId })
            .IsUnique()
            .HasDatabaseName("uq_series_vote_input_period_series");

        builder.HasOne(v => v.PublicationPeriod)
            .WithMany()
            .HasForeignKey(v => v.PublicationPeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.Series)
            .WithMany()
            .HasForeignKey(v => v.SeriesId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.EnteredByUser)
            .WithMany()
            .HasForeignKey(v => v.EnteredByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.UpdatedByUser)
            .WithMany()
            .HasForeignKey(v => v.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
