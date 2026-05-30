using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaManagementSystem.Infrastructure.Persistence.Configurations
{
    public class SeriesConfiguration : IEntityTypeConfiguration<Series>
    {
        public void Configure(EntityTypeBuilder<Series> builder)
        {
            builder.ToTable("Series", "manga");
            builder.HasKey(s => s.SeriesId);
            builder.Property(s => s.SeriesId).ValueGeneratedOnAdd();
            builder.Property(s => s.SeriesCode).IsRequired().HasMaxLength(50);
            builder.Property(s => s.Title).HasMaxLength(200);
            builder.Property(s => s.Slug).HasMaxLength(220);
            builder.Property(s => s.Synopsis);
            builder.Property(s => s.Genre).HasMaxLength(100);
            builder.Property(s => s.StatusCode).HasMaxLength(50).HasDefaultValue("PROPOSAL_DRAFT");
            builder.Property(s => s.ContentLanguageCode).HasMaxLength(10).HasDefaultValue("ja");
            builder.Property(s => s.PublicationFrequencyCode).HasMaxLength(20);
            builder.Property(s => s.CreatedAtUtc).IsRequired();
            builder.HasIndex(s => s.SeriesCode).IsUnique();
            builder.HasIndex(s => s.Slug).IsUnique();
            builder.HasIndex(s => s.StatusCode).HasDatabaseName("ix_series_current_status_code");
            builder.HasCheckConstraint("CK_Series_StatusCode", "[StatusCode] IN ('PROPOSAL_DRAFT','ACTIVE','CANCELLED','ARCHIVED')");
            builder.HasCheckConstraint("CK_Series_ContentLanguageCode", "[ContentLanguageCode] IN ('ja','en','vi')");
            builder.HasCheckConstraint("CK_Series_PublicationFrequencyCode", "[PublicationFrequencyCode] IS NULL OR [PublicationFrequencyCode] IN ('WEEKLY','MONTHLY','IRREGULAR')");
            builder.HasOne(s => s.CoverFile).WithMany().HasForeignKey(s => s.CoverFileId);
            builder.HasOne(s => s.SourceSeries).WithMany().HasForeignKey(s => s.SourceSeriesId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(s => s.UpdatedByUser).WithMany().HasForeignKey(s => s.UpdatedByUserId);
        }
    }
}
