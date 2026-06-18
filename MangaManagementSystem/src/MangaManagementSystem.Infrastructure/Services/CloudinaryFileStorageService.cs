using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Infrastructure.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;


namespace MangaManagementSystem.Infrastructure.Services
{
    public class CloudinaryFileStorageService : IFileStorageService
    {
        private readonly Cloudinary _cloudinary;
        private readonly CloudinarySettings _settings;

        private static readonly string[] AllowedImageContentTypes =
        {
            "image/png",
            "image/jpeg",
            "image/jpg",
            "image/gif",
            "image/webp"
        };

        private static readonly string[] AllowedRawContentTypes =
        {
            "application/pdf",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/msword"
        };

        private const long MaxFileSizeBytes = 10 * 1024 * 1024;

        private static readonly string[] ValidPurposes =
        {
            "SERIES_PROPOSAL",
            "SERIES_COVER",
            "CHAPTER_PAGE_VERSION",
            "EDITORIAL_ATTACHMENT",
            "REGISTRATION_PORTFOLIO",
            "USER_AVATAR"
        };

        public CloudinaryFileStorageService(Cloudinary cloudinary, IOptions<CloudinarySettings> options)
        {
            _cloudinary = cloudinary ?? throw new ArgumentNullException(nameof(cloudinary));
            _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<FileUploadResultDto> UploadFileAsync(
            IFormFile file,
            string filePurposeCode,
            int? uploadedByUserId = null)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            if (string.IsNullOrWhiteSpace(filePurposeCode))
            {
                throw new ArgumentException("File purpose is required.", nameof(filePurposeCode));
            }

            if (!ValidPurposes.Contains(filePurposeCode))
            {
                throw new InvalidOperationException($"Invalid file purpose code: {filePurposeCode}");
            }

            if (file.Length <= 0)
            {
                throw new InvalidOperationException("File is empty.");
            }

            if (file.Length > MaxFileSizeBytes)
            {
                throw new InvalidOperationException($"File exceeds maximum allowed size of {MaxFileSizeBytes} bytes.");
            }

            var contentType = file.ContentType?.ToLowerInvariant() ?? string.Empty;
            var isImage = AllowedImageContentTypes.Contains(contentType);
            var isRaw = AllowedRawContentTypes.Contains(contentType);

            ValidateFileTypeForPurpose(filePurposeCode, isImage, isRaw);

            byte[] fileBytes;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                fileBytes = ms.ToArray();
            }

            var sha256Hash = ComputeSha256Hash(fileBytes);
            var originalFileName = Path.GetFileName(file.FileName);
            var folder = BuildFolderForPurpose(filePurposeCode);

            var uploadResult = await UploadToCloudinaryAsync(
                fileBytes,
                originalFileName,
                contentType,
                folder,
                isImage);

            return new FileUploadResultDto(
                uploadResult.PublicId ?? string.Empty,
                uploadResult.SecureUrl?.ToString() ?? uploadResult.Url?.ToString() ?? string.Empty,
                contentType,
                fileBytes.LongLength,
                originalFileName,
                sha256Hash
            );
        }

        public async Task<FileUploadResultDto> UploadFileAsync(
            byte[] fileBytes,
            string originalFileName,
            string contentType,
            string filePurposeCode,
            int? uploadedByUserId = null)
        {
            if (fileBytes == null)
            {
                throw new ArgumentNullException(nameof(fileBytes));
            }

            if (string.IsNullOrWhiteSpace(originalFileName))
            {
                throw new ArgumentException("Original file name is required.", nameof(originalFileName));
            }

            if (string.IsNullOrWhiteSpace(filePurposeCode))
            {
                throw new ArgumentException("File purpose is required.", nameof(filePurposeCode));
            }

            if (!ValidPurposes.Contains(filePurposeCode))
            {
                throw new InvalidOperationException($"Invalid file purpose code: {filePurposeCode}");
            }

            if (fileBytes.LongLength <= 0)
            {
                throw new InvalidOperationException("File is empty.");
            }

            if (fileBytes.LongLength > MaxFileSizeBytes)
            {
                throw new InvalidOperationException($"File exceeds maximum allowed size of {MaxFileSizeBytes} bytes.");
            }

            var normalizedContentType = contentType?.ToLowerInvariant() ?? string.Empty;
            var isImage = AllowedImageContentTypes.Contains(normalizedContentType);
            var isRaw = AllowedRawContentTypes.Contains(normalizedContentType);

            ValidateFileTypeForPurpose(filePurposeCode, isImage, isRaw);

            var sha256Hash = ComputeSha256Hash(fileBytes);
            var safeOriginalFileName = Path.GetFileName(originalFileName);
            var folder = BuildFolderForPurpose(filePurposeCode);

            var uploadResult = await UploadToCloudinaryAsync(
                fileBytes,
                safeOriginalFileName,
                normalizedContentType,
                folder,
                isImage);

            return new FileUploadResultDto(
                uploadResult.PublicId ?? string.Empty,
                uploadResult.SecureUrl?.ToString() ?? uploadResult.Url?.ToString() ?? string.Empty,
                normalizedContentType,
                fileBytes.LongLength,
                safeOriginalFileName,
                sha256Hash
            );
        }

        public async Task DeleteFileAsync(string publicId, string resourceType)
        {
            if (string.IsNullOrWhiteSpace(publicId))
            {
                return;
            }

            var delParams = new DeletionParams(publicId)
            {
                ResourceType = resourceType == "raw" ? ResourceType.Raw : ResourceType.Image
            };

            await _cloudinary.DestroyAsync(delParams);
        }

        private async Task<UploadResult> UploadToCloudinaryAsync(
            byte[] fileBytes,
            string originalFileName,
            string contentType,
            string folder,
            bool isImage)
        {
            UploadResult? uploadResult;

            if (isImage)
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(originalFileName, new MemoryStream(fileBytes)),
                    Folder = folder,
                    UseFilename = true,
                    UniqueFilename = true,
                    Overwrite = false
                };

                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }
            else
            {
                var uploadParams = new RawUploadParams
                {
                    File = new FileDescription(originalFileName, new MemoryStream(fileBytes)),
                    Folder = folder,
                    UseFilename = true,
                    UniqueFilename = true,
                    Overwrite = false
                };

                var rawResult = await _cloudinary.UploadAsync(uploadParams);
                uploadResult = rawResult as UploadResult;
            }

            if (uploadResult == null || uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new InvalidOperationException("Cloudinary upload failed.");
            }

            return uploadResult;
        }

        private static void ValidateFileTypeForPurpose(string filePurposeCode, bool isImage, bool isRaw)
        {
            if (filePurposeCode == "USER_AVATAR" && !isImage)
            {
                throw new InvalidOperationException("Avatar upload only supports image files.");
            }

            if (filePurposeCode == "REGISTRATION_PORTFOLIO" && !isRaw)
            {
                throw new InvalidOperationException("Portfolio upload only supports PDF, DOC, or DOCX files.");
            }

            if (filePurposeCode == "SERIES_COVER" && !isImage)
            {
                throw new InvalidOperationException("Series cover upload only supports image files.");
            }

            if (filePurposeCode == "CHAPTER_PAGE_VERSION" && !isImage)
            {
                throw new InvalidOperationException("Chapter page upload only supports image files.");
            }

            if (!isImage && !isRaw)
            {
                throw new InvalidOperationException("Unsupported file type.");
            }
        }

        private static string BuildFolderForPurpose(string purpose) => purpose switch
        {
            "REGISTRATION_PORTFOLIO" => "registration_portfolios",
            "USER_AVATAR" => "avatars",
            "SERIES_COVER" => "series/covers",
            "SERIES_PROPOSAL" => "series/proposals",
            "CHAPTER_PAGE_VERSION" => "chapters/pages",
            "EDITORIAL_ATTACHMENT" => "editorial/attachments",
            _ => "misc"
        };

        private static string ComputeSha256Hash(byte[] fileBytes)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(fileBytes);
            return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}