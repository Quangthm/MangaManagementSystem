using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Services
{
    public class FileResourceService : IFileResourceService
    {
        private const string CloudinaryRawResourceType = "raw";
        private const string CloudinaryImageResourceType = "image";
        private const int MaxCleanupErrorLength = 1000;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService;

        public FileResourceService(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorageService)
        {
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
        }

        public async Task<FileResourceDto> CreateFileResourceAsync(CreateFileResourceDto dto)
        {
            var entity = new FileResource
            {
                FilePurposeCode = dto.FilePurposeCode,
                OriginalFileName = dto.OriginalFileName,
                CloudinaryPublicId = dto.CloudinaryPublicId,
                CloudinarySecureUrl = dto.CloudinarySecureUrl,
                ContentType = dto.ContentType,
                FileSizeBytes = dto.FileSizeBytes,
                Sha256Hash = dto.Sha256Hash,
                UploadedByUserId = dto.UploadedByUserId,
                UploadedAtUtc = DateTime.UtcNow
            };

            await _unitOfWork.FileResources.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(entity);
        }

        public async Task<FileResourceDto?> GetFileResourceByIdAsync(Guid id)
        {
            var entity = await _unitOfWork.FileResources.GetByIdAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<IEnumerable<FileResourceDto>> GetAllFileResourcesAsync()
        {
            var entities = await _unitOfWork.FileResources.GetAllAsync();

            return entities
                .Where(f => f.DeletedAtUtc == null)
                .Select(MapToDto);
        }

        public async Task<bool> DeleteFileResourceAsync(Guid id, Guid? deletedByUserId = null)
        {
            var entity = await _unitOfWork.FileResources.GetByIdAsync(id);
            if (entity == null || entity.DeletedAtUtc != null)
            {
                return false;
            }

            entity.DeletedAtUtc = DateTime.UtcNow;
            entity.DeletedByUserId = deletedByUserId;
            entity.StorageCleanedAtUtc = null;
            entity.StorageCleanupError = null;

            _unitOfWork.FileResources.Update(entity);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<AdminFileResourceSearchResultDto> SearchAdminFilesAsync(
            string? keyword = null,
            string? purposeCode = null,
            string? storageState = null,
            int page = 1,
            int pageSize = 20)
        {
            var normalizedPage = NormalizePage(page);
            var normalizedPageSize = NormalizePageSize(pageSize);

            var items = await _unitOfWork.FileResources.SearchAdminFilesAsync(
                keyword,
                purposeCode,
                storageState,
                normalizedPage,
                normalizedPageSize);

            var totalCount = await _unitOfWork.FileResources.CountAdminFilesAsync(
                keyword,
                purposeCode,
                storageState);

            return new AdminFileResourceSearchResultDto(
                items.Select(MapToDto).ToList(),
                totalCount,
                normalizedPage,
                normalizedPageSize);
        }

        public async Task<FileStorageCleanupResultDto> CleanupStorageAsync(Guid fileResourceId)
        {
            var entity = await _unitOfWork.FileResources.GetByIdAsync(fileResourceId);
            if (entity == null)
            {
                return new FileStorageCleanupResultDto(
                    fileResourceId,
                    "-",
                    "Not found",
                    Succeeded: false,
                    StorageObjectNotFound: false,
                    Message: "File resource was not found.");
            }

            if (entity.DeletedAtUtc == null)
            {
                return new FileStorageCleanupResultDto(
                    entity.FileResourceId,
                    entity.OriginalFileName,
                    MapToDto(entity).StorageState,
                    Succeeded: false,
                    StorageObjectNotFound: false,
                    Message: "Only soft-deleted file resources can be cleaned up.");
            }

            if (entity.StorageCleanedAtUtc.HasValue)
            {
                return new FileStorageCleanupResultDto(
                    entity.FileResourceId,
                    entity.OriginalFileName,
                    MapToDto(entity).StorageState,
                    Succeeded: true,
                    StorageObjectNotFound: false,
                    Message: "Storage object was already cleaned.");
            }

            try
            {
                var resourceType = DetermineCloudinaryResourceType(entity.ContentType);
                var deleteResult = await _fileStorageService.DeleteFileAsync(
                    entity.CloudinaryPublicId,
                    resourceType);

                entity.StorageCleanedAtUtc = DateTime.UtcNow;
                entity.StorageCleanupError = null;

                _unitOfWork.FileResources.Update(entity);
                await _unitOfWork.SaveChangesAsync();

                var storageObjectNotFound = deleteResult.NotFound;
                var message = storageObjectNotFound
                    ? "Storage object was already missing, so cleanup was marked as completed."
                    : "Storage object was deleted successfully.";

                return new FileStorageCleanupResultDto(
                    entity.FileResourceId,
                    entity.OriginalFileName,
                    MapToDto(entity).StorageState,
                    Succeeded: true,
                    StorageObjectNotFound: storageObjectNotFound,
                    Message: message);
            }
            catch (Exception ex)
            {
                entity.StorageCleanupError = TruncateCleanupError(ex.Message);
                _unitOfWork.FileResources.Update(entity);
                await _unitOfWork.SaveChangesAsync();

                return new FileStorageCleanupResultDto(
                    entity.FileResourceId,
                    entity.OriginalFileName,
                    MapToDto(entity).StorageState,
                    Succeeded: false,
                    StorageObjectNotFound: false,
                    Message: entity.StorageCleanupError);
            }
        }

        public async Task<IReadOnlyList<FileStorageCleanupResultDto>> CleanupDeletedStorageBatchAsync(
            int batchSize = 20)
        {
            var candidates = await _unitOfWork.FileResources.GetStorageCleanupCandidatesAsync(batchSize);
            var results = new List<FileStorageCleanupResultDto>();

            foreach (var candidate in candidates)
            {
                var result = await CleanupStorageAsync(candidate.FileResourceId);
                results.Add(result);
            }

            return results;
        }

        private static FileResourceDto MapToDto(FileResource f) => new(
            f.FileResourceId,
            f.FilePurposeCode,
            f.OriginalFileName,
            f.CloudinaryPublicId,
            f.CloudinarySecureUrl,
            f.ContentType,
            f.FileSizeBytes,
            f.Sha256Hash,
            f.UploadedByUserId,
            f.UploadedAtUtc,
            f.DeletedAtUtc,
            f.DeletedByUserId,
            f.StorageCleanedAtUtc,
            f.StorageCleanupError
        );

        private static string DetermineCloudinaryResourceType(string contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType))
            {
                return CloudinaryImageResourceType;
            }

            return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                ? CloudinaryImageResourceType
                : CloudinaryRawResourceType;
        }

        private static string TruncateCleanupError(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return "Unknown storage cleanup error.";
            }

            return message.Length <= MaxCleanupErrorLength
                ? message
                : message[..MaxCleanupErrorLength];
        }

        private static int NormalizePage(int page)
        {
            return page < 1 ? 1 : page;
        }

        private static int NormalizePageSize(int pageSize)
        {
            if (pageSize < 1)
            {
                return 20;
            }

            return pageSize > 100 ? 100 : pageSize;
        }
    }
}
