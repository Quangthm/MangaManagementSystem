using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace MangaManagementSystem.Infrastructure.Services
{
    public class CloudinaryFileStorageService : IFileStorageService
    {
        private readonly Cloudinary _cloudinary;
        private readonly CloudinarySettings _settings;

        private static readonly string[] AllowedImageContentTypes = new[] { "image/png", "image/jpeg", "image/jpg", "image/gif", "image/webp" };
        private static readonly string[] AllowedRawContentTypes = new[] { "application/pdf", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/msword" };
        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB, adjust if needed

        private static readonly string[] ValidPurposes = new[] {
            "SERIES_PROPOSAL",
            "SERIES_COVER",
            "CHAPTER_PAGE_VERSION",
            "TASK_REFERENCE",
            "EDITORIAL_ATTACHMENT",
            "REGISTRATION_PORTFOLIO",
            "USER_AVATAR"
        };

        public CloudinaryFileStorageService(Cloudinary cloudinary, IOptions<CloudinarySettings> options)
        {
            _cloudinary = cloudinary ?? throw new ArgumentNullException(nameof(cloudinary));
            _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<FileUploadResultDto> UploadFileAsync(IFormFile file, string filePurposeCode, int? uploadedByUserId = null)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));
            if (string.IsNullOrWhiteSpace(filePurposeCode)) throw new ArgumentException("File purpose is required.", nameof(filePurposeCode));

            if (!ValidPurposes.Contains(filePurposeCode))
            {
                throw new InvalidOperationException($"Invalid file purpose code: {filePurposeCode}");
            }

            if (file.Length <= 0) throw new InvalidOperationException("File is empty.");
            if (file.Length > MaxFileSizeBytes) throw new InvalidOperationException($"File exceeds maximum allowed size of {MaxFileSizeBytes} bytes.");

            var contentType = file.ContentType?.ToLowerInvariant() ?? string.Empty;

            var isImage = AllowedImageContentTypes.Contains(contentType);
            var isRaw = AllowedRawContentTypes.Contains(contentType);

            if (!isImage && !isRaw)
            {
                throw new InvalidOperationException("Unsupported file type.");
            }

            // Read bytes and compute SHA-256
            byte[] fileBytes;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                fileBytes = ms.ToArray();
            }

            string sha256Hash;
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(fileBytes);
                sha256Hash = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            }

            // Prepare upload params
            var originalFileName = Path.GetFileName(file.FileName);
            var uploadResult = (UploadResult?)null;

            if (isImage)
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(originalFileName, new MemoryStream(fileBytes)),
                    UseFilename = true,
                    UniqueFilename = false,
                    Overwrite = false,
                    Folder = BuildFolderForPurpose(filePurposeCode)
                };

                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }
            else
            {
                var uploadParams = new RawUploadParams
                {
                    File = new FileDescription(originalFileName, new MemoryStream(fileBytes)),
                    UseFilename = true,
                    UniqueFilename = false,
                    Overwrite = false,
                    Folder = BuildFolderForPurpose(filePurposeCode)
                };

                var rawResult = await _cloudinary.UploadAsync(uploadParams);
                // raw uploads return RawUploadResult which inherits from UploadResult
                uploadResult = rawResult as UploadResult;
            }

            if (uploadResult == null || uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new InvalidOperationException("Cloudinary upload failed.");
            }

            return new FileUploadResultDto(
                uploadResult.PublicId ?? string.Empty,
                uploadResult.SecureUrl?.ToString() ?? uploadResult.Url?.ToString() ?? string.Empty,
                contentType,
                fileBytes.LongLength,
                originalFileName,
                sha256Hash
            );
        }

        public async Task DeleteFileAsync(string publicId, string resourceType)
        {
            if (string.IsNullOrEmpty(publicId)) return;

            var delParams = new DeletionParams(publicId)
            {
                ResourceType = resourceType == "raw" ? ResourceType.Raw : ResourceType.Image
            };

            var result = await _cloudinary.DestroyAsync(delParams);
            // result.Result can be "ok" or "not found" etc. We don't throw here; caller will decide.
        }

        public async Task<FileUploadResultDto> UploadFileAsync(byte[] fileBytes, string originalFileName, string contentType, string filePurposeCode, int? uploadedByUserId = null)
        {
            if (fileBytes == null) throw new ArgumentNullException(nameof(fileBytes));
            if (string.IsNullOrWhiteSpace(originalFileName)) throw new ArgumentException("Original file name is required.", nameof(originalFileName));
            if (string.IsNullOrWhiteSpace(filePurposeCode)) throw new ArgumentException("File purpose is required.", nameof(filePurposeCode));

            if (!ValidPurposes.Contains(filePurposeCode))
            {
                throw new InvalidOperationException($"Invalid file purpose code: {filePurposeCode}");
            }

            if (fileBytes.LongLength <= 0) throw new InvalidOperationException("File is empty.");
            if (fileBytes.LongLength > MaxFileSizeBytes) throw new InvalidOperationException($"File exceeds maximum allowed size of {MaxFileSizeBytes} bytes.");

            var ct = contentType?.ToLowerInvariant() ?? string.Empty;

            var isImage = AllowedImageContentTypes.Contains(ct);
            var isRaw = AllowedRawContentTypes.Contains(ct);

            if (!isImage && !isRaw)
            {
                throw new InvalidOperationException("Unsupported file type.");
            }

            string sha256Hash;
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(fileBytes);
                sha256Hash = BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            }

            var uploadResult = (UploadResult?)null;

            if (isImage)
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(originalFileName, new MemoryStream(fileBytes)),
                    UseFilename = true,
                    UniqueFilename = false,
                    Overwrite = false,
                    Folder = BuildFolderForPurpose(filePurposeCode)
                };

                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }
            else
            {
                var uploadParams = new RawUploadParams
                {
                    File = new FileDescription(originalFileName, new MemoryStream(fileBytes)),
                    UseFilename = true,
                    UniqueFilename = false,
                    Overwrite = false,
                    Folder = BuildFolderForPurpose(filePurposeCode)
                };

                var rawResult = await _cloudinary.UploadAsync(uploadParams);
                uploadResult = rawResult as UploadResult;
            }

            if (uploadResult == null || uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new InvalidOperationException("Cloudinary upload failed.");
            }

            return new FileUploadResultDto(
                uploadResult.PublicId ?? string.Empty,
                uploadResult.SecureUrl?.ToString() ?? uploadResult.Url?.ToString() ?? string.Empty,
                contentType ?? "application/octet-stream",
                fileBytes.LongLength,
                originalFileName,
                sha256Hash
            );
        }

        private static string BuildFolderForPurpose(string purpose) => purpose switch
        {
            "REGISTRATION_PORTFOLIO" => "registration_portfolios",
            "USER_AVATAR" => "avatars",
            "SERIES_COVER" => "series/covers",
            "SERIES_PROPOSAL" => "series/proposals",
            "CHAPTER_PAGE_VERSION" => "chapters/pages",
            "TASK_REFERENCE" => "tasks/references",
            "EDITORIAL_ATTACHMENT" => "editorial/attachments",
            _ => "misc"
        };
    }
}
