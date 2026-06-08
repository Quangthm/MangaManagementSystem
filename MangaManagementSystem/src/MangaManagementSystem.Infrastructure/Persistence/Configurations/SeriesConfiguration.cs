using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaManagementSystem.Infrastructure.Persistence.Configurations
{
    public class SeriesConfiguration : IEntityTypeConfiguration<Series>
    {
        public void Configure(EntityTypeBuilder<Series> builder)
        {
            builder.ToTable("series", "manga", t =>
            {
                // FR-SERIES-004:
                // Series chỉ được có một lifecycle status hợp lệ.
                t.HasCheckConstraint(
                    "CK_Series_StatusCode",
                    "status_code IN ('PROPOSAL_DRAFT','UNDER_EDITORIAL_REVIEW','UNDER_BOARD_REVIEW','SERIALIZED','HIATUS','CANCELLED','COMPLETED')"
                );

                // FR-SERIES-006:
                // Series bắt buộc có một primary content language hợp lệ.
                t.HasCheckConstraint(
                    "CK_Series_ContentLanguageCode",
                    "content_language_code IN ('ja','en','vi')"
                );

                // Publication frequency optional.
                t.HasCheckConstraint(
                    "CK_Series_PublicationFrequencyCode",
                    "publication_frequency_code IS NULL OR publication_frequency_code IN ('WEEKLY','MONTHLY','IRREGULAR')"
                );

                // FR-SERIES-011:
                // Series không được tự reference chính nó làm source series.
                t.HasCheckConstraint(
                    "CK_Series_NotSelfSource",
                    "source_series_id IS NULL OR source_series_id <> series_id"
                );

                // FR-SERIES-016:
                // Không được có metadata update bị thiếu một vế.
                // Nếu có UpdatedAtUtc thì phải có UpdatedByUserId, và ngược lại.
                t.HasCheckConstraint(
                    "CK_Series_UpdateMetadata_Complete",
                    "(updated_at_utc IS NULL AND updated_by_user_id IS NULL) OR (updated_at_utc IS NOT NULL AND updated_by_user_id IS NOT NULL)"
                );
            });

            builder.HasKey(s => s.SeriesId);

            builder.Property(s => s.SeriesId)
                .ValueGeneratedOnAdd();

            // FR-SERIES-002:
            // SeriesCode là system code và phải unique.
            builder.Property(s => s.SeriesCode)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(s => s.SeriesCode)
                .HasDatabaseName("ux_series_code")
                .IsUnique();

            builder.Property(s => s.Title)
                .IsRequired()
                .HasMaxLength(200);

            // FR-SERIES-003:
            // Slug phải unique.
            builder.Property(s => s.Slug)
                .IsRequired()
                .HasMaxLength(220);

            builder.HasIndex(s => s.Slug)
                .HasDatabaseName("ux_series_slug")
                .IsUnique();

            builder.Property(s => s.Synopsis)
                .IsRequired();

            // FR-SERIES-007:
            // Genre chỉ là simple text metadata trong MVP.
            builder.Property(s => s.Genre)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(s => s.StatusCode)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("PROPOSAL_DRAFT");

            builder.Property(s => s.ContentLanguageCode)
                .IsRequired()
                .HasMaxLength(10)
                .HasDefaultValue("ja");

            builder.Property(s => s.PublicationFrequencyCode)
                .HasMaxLength(20);

            builder.Property(s => s.CreatedAtUtc)
                .IsRequired();

            builder.Property(s => s.UpdatedAtUtc);

            builder.Property(s => s.UpdatedByUserId);

            // Index hỗ trợ query theo status hiện tại.
            builder.HasIndex(s => s.StatusCode)
                .HasDatabaseName("ix_series_current_status_code");

            // FR-SERIES-008:
            // Series có thể reference optional cover image thông qua FileResource.
            // FR-SERIES-009 cover purpose SERIES_COVER sẽ validate ở Service,
            // vì database check constraint không kiểm tra được value ở bảng FileResource khác.
            builder.HasOne(s => s.CoverFile)
                .WithMany()
                .HasForeignKey(s => s.CoverFileId);

            // FR-SERIES-010:
            // Series có thể reference optional source series.
            builder.HasOne(s => s.SourceSeries)
                .WithMany()
                .HasForeignKey(s => s.SourceSeriesId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.UpdatedByUser)
                .WithMany()
                .HasForeignKey(s => s.UpdatedByUserId);
        }
    }
}