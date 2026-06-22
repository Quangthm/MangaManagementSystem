using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaManagementSystem.Infrastructure.Persistence.Configurations
{
    public class SeriesConfiguration : IEntityTypeConfiguration<Series>
    {
        public void Configure(EntityTypeBuilder<Series> builder)
        {
            builder.ToTable("Series", "manga", t =>
            {
                t.HasCheckConstraint("CK_Series_StatusCode", "status_code IN ('PROPOSAL_DRAFT','UNDER_EDITORIAL_REVIEW','UNDER_BOARD_REVIEW','SERIALIZED','HIATUS','CANCELLED','COMPLETED')");
                t.HasCheckConstraint("CK_Series_ContentLanguageCode", "content_language_code IN ('ja','en','vi')");
                t.HasCheckConstraint("CK_Series_PublicationFrequencyCode", "publication_frequency_code IS NULL OR publication_frequency_code IN ('WEEKLY','MONTHLY','IRREGULAR')");
            });
            builder.HasKey(s => s.SeriesId);
            builder.Property(s => s.SeriesId).ValueGeneratedOnAdd();
            builder.Property(s => s.Title).IsRequired().HasMaxLength(200);
            builder.Property(s => s.Slug).IsRequired().HasMaxLength(220);
            builder.Property(s => s.Synopsis).IsRequired();
            builder.Property(s => s.StatusCode).HasMaxLength(50).HasDefaultValue("PROPOSAL_DRAFT");
            builder.Property(s => s.ContentLanguageCode).HasMaxLength(10).HasDefaultValue("ja");
            builder.Property(s => s.PublicationFrequencyCode).HasMaxLength(20);
            builder.Property(s => s.CreatedAtUtc).IsRequired();
            builder.HasIndex(s => s.Slug).IsUnique();
            builder.HasIndex(s => s.StatusCode).HasDatabaseName("ix_series_current_status_code");
            // Check constraints moved to ToTable above per EF Core guidance
            builder.HasOne(s => s.CoverFile).WithMany().HasForeignKey(s => s.CoverFileId);
            builder.HasOne(s => s.SourceSeries).WithMany().HasForeignKey(s => s.SourceSeriesId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(s => s.UpdatedByUser).WithMany().HasForeignKey(s => s.UpdatedByUserId);
        }
    }
}
