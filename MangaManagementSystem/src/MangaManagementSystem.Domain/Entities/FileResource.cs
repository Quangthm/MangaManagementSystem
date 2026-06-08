using MangaManagementSystem.Domain.Common;
using System;
using System.Collections.Generic;

namespace MangaManagementSystem.Domain.Entities
{
public class FileResource : BaseEntity
{
    public Guid FileResourceId { get; set; }
        public string FilePurposeCode { get; set; } = null!;
        public string OriginalFileName { get; set; } = null!;
        public string CloudinaryPublicId { get; set; } = null!;
        public string CloudinarySecureUrl { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long FileSizeBytes { get; set; }
        public string? Sha256Hash { get; set; }
        public Guid? UploadedByUserId { get; set; }
        public User? UploadedByUser { get; set; }
        public DateTime UploadedAtUtc { get; set; }
        public DateTime? DeletedAtUtc { get; set; }
        public Guid? DeletedByUserId { get; set; }
        public User? DeletedByUser { get; set; }
    }
}
