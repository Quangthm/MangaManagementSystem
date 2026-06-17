using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;

namespace MangaManagementSystem.Application.Services
{
    public class FileResourceService
        : IFileResourceService
    {
        private readonly IUnitOfWork _unitOfWork;

        public FileResourceService(
            IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<FileResourceDto>
            CreateFileResourceAsync(
                CreateFileResourceDto dto)
        {
            var entity =
                new FileResource
                {
                    FilePurposeCode =
                        dto.FilePurposeCode,

                    OriginalFileName =
                        dto.OriginalFileName,

                    CloudinaryPublicId =
                        dto.CloudinaryPublicId,

                    CloudinarySecureUrl =
                        dto.CloudinarySecureUrl,

                    ContentType =
                        dto.ContentType,

                    FileSizeBytes =
                        dto.FileSizeBytes,

                    Sha256Hash =
                        dto.Sha256Hash,

                    UploadedByUserId =
                        dto.UploadedByUserId,

                    UploadedAtUtc =
                        DateTime.UtcNow
                };

            await _unitOfWork.FileResources
                .AddAsync(entity);

            await _unitOfWork.SaveChangesAsync();

            return MapToDto(entity);
        }

        public async Task<FileResourceDto?>
            GetFileResourceByIdAsync(
                Guid id)
        {
            var entity =
                await _unitOfWork.FileResources
                    .GetByIdAsync(id);

            return entity is null
                ? null
                : MapToDto(entity);
        }

        public async Task<IEnumerable<FileResourceDto>>
            GetAllFileResourcesAsync()
        {
            var entities =
                await _unitOfWork.FileResources
                    .GetAllAsync();

            return entities
                .Where(file =>
                    file.DeletedAtUtc is null)
                .Select(MapToDto);
        }

        public async Task<bool>
            DeleteFileResourceAsync(
                Guid id,
                Guid actorUserId,
                string actorRoleName)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException(
                    "File resource id is required.",
                    nameof(id));
            }

            if (actorUserId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Actor user id is required.",
                    nameof(actorUserId));
            }

            if (string.IsNullOrWhiteSpace(
                    actorRoleName))
            {
                throw new ArgumentException(
                    "Actor role is required.",
                    nameof(actorRoleName));
            }

            var entity =
                await _unitOfWork.FileResources
                    .GetByIdAsync(id);

            if (entity is null
                || entity.DeletedAtUtc is not null)
            {
                return false;
            }

            var actorIsOwner =
                entity.UploadedByUserId.HasValue
                && entity.UploadedByUserId.Value
                    == actorUserId;

            var actorIsAdmin =
                string.Equals(
                    actorRoleName.Trim(),
                    "Admin",
                    StringComparison.OrdinalIgnoreCase);

            if (!actorIsOwner
                && !actorIsAdmin)
            {
                throw new UnauthorizedAccessException(
                    "You do not have permission to delete this file.");
            }

            entity.DeletedAtUtc =
                DateTime.UtcNow;

            entity.DeletedByUserId =
                actorUserId;

            _unitOfWork.FileResources
                .Update(entity);

            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        private static FileResourceDto MapToDto(
            FileResource file)
        {
            return new FileResourceDto(
                file.FileResourceId,
                file.FilePurposeCode,
                file.OriginalFileName,
                file.CloudinaryPublicId,
                file.CloudinarySecureUrl,
                file.ContentType,
                file.FileSizeBytes,
                file.Sha256Hash,
                file.UploadedByUserId,
                file.UploadedAtUtc,
                file.DeletedAtUtc,
                file.DeletedByUserId);
        }
    }
}
