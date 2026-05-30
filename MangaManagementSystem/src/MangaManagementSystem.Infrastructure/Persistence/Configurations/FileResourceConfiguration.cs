using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaManagementSystem.Infrastructure.Persistence.Configurations
{
    public class FileResourceConfiguration : IEntityTypeConfiguration<FileResource>
    {
        public void Configure(EntityTypeBuilder<FileResource> builder)
        {
            builder.ToTable("FileResource", "manga");
            builder.HasKey(f => f.FileResourceId);
            builder.Property(f => f.FileResourceId).ValueGeneratedOnAdd();
            builder.Property(f => f.FilePurposeCode).IsRequired().HasMaxLength(50);
            builder.Property(f => f.OriginalFileName).IsRequired().HasMaxLength(260);
            builder.Property(f => f.CloudinaryPublicId).IsRequired().HasMaxLength(255);
            builder.Property(f => f.CloudinarySecureUrl).IsRequired().HasMaxLength(1000);
            builder.Property(f => f.ContentType).IsRequired().HasMaxLength(100);
            builder.Property(f => f.FileSizeBytes).IsRequired();
            builder.Property(f => f.Sha256Hash).HasMaxLength(64);
            builder.Property(f => f.UploadedAtUtc).HasDefaultValueSql("SYSUTCDATETIME()");
            builder.HasIndex(f => f.FilePurposeCode).HasDatabaseName("ix_file_resource_purpose_code");
            builder.HasIndex(f => f.UploadedByUserId).HasDatabaseName("ix_file_resource_uploaded_by");
            builder.HasIndex(f => new { f.FilePurposeCode, f.DeletedAtUtc }).HasDatabaseName("ix_file_resource_active_by_purpose").HasFilter("[DeletedAtUtc] IS NULL");
            builder.HasOne(f => f.UploadedByUser).WithMany().HasForeignKey(f => f.UploadedByUserId);
            builder.HasOne(f => f.DeletedByUser).WithMany().HasForeignKey(f => f.DeletedByUserId);
            builder.HasCheckConstraint("CK_FileResource_PurposeCode", "[FilePurposeCode] IN ('COVER','PORTFOLIO','PAGE','MARKUP','OTHER')");
        }
    }
}
