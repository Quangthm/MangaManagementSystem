using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaManagementSystem.Infrastructure.Persistence.Configurations
{
    public class FileResourceConfiguration : IEntityTypeConfiguration<FileResource>
    {
        public void Configure(EntityTypeBuilder<FileResource> builder)
        {
            builder.ToTable("FileResource", "manga", tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "ck_file_resource_file_purpose_code",
                    "file_purpose_code IN ('SERIES_PROPOSAL','SERIES_COVER','CHAPTER_PAGE_VERSION','EDITORIAL_ATTACHMENT','REGISTRATION_PORTFOLIO','USER_AVATAR')");

                tableBuilder.HasCheckConstraint(
                    "ck_file_resource_file_size_positive",
                    "file_size_bytes > 0");

                tableBuilder.HasCheckConstraint(
                    "ck_file_resource_storage_cleanup_status",
                    "storage_cleanup_status IN ('AVAILABLE','CLEANED','MISSING','FAILED')");
            });

            builder.HasKey(f => f.FileResourceId);

            builder.Property(f => f.FileResourceId)
                .ValueGeneratedOnAdd();

            builder.Property(f => f.FilePurposeCode)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(f => f.OriginalFileName)
                .IsRequired()
                .HasMaxLength(260);

            builder.Property(f => f.CloudinaryPublicId)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(f => f.CloudinarySecureUrl)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(f => f.ContentType)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(f => f.FileSizeBytes)
                .IsRequired();

            builder.Property(f => f.Sha256Hash)
                .HasColumnName("sha256_hash")
                .HasMaxLength(64);

            builder.Property(f => f.UploadedAtUtc)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            builder.Property(f => f.StorageCleanupStatus)
                .IsRequired()
                .HasMaxLength(20)
                .HasDefaultValue("AVAILABLE");

            builder.Property(f => f.StorageCleanedAtUtc);

            builder.Property(f => f.StorageCleanedByUserId);

            builder.Property(f => f.StorageCleanupError)
                .HasMaxLength(1000);

            builder.HasIndex(f => f.FilePurposeCode)
                .HasDatabaseName("ix_file_resource_purpose_code");

            builder.HasIndex(f => f.UploadedByUserId)
                .HasDatabaseName("ix_file_resource_uploaded_by");

            builder.HasIndex(f => new
            {
                f.FilePurposeCode,
                f.DeletedAtUtc
            })
                .HasDatabaseName("ix_file_resource_active_by_purpose")
                .HasFilter("deleted_at_utc IS NULL");

            builder.HasOne(f => f.UploadedByUser)
                .WithMany()
                .HasForeignKey(f => f.UploadedByUserId);

            builder.HasOne(f => f.DeletedByUser)
                .WithMany()
                .HasForeignKey(f => f.DeletedByUserId);

            builder.HasOne(f => f.StorageCleanedByUser)
                .WithMany()
                .HasForeignKey(f => f.StorageCleanedByUserId);
        }
    }
}
