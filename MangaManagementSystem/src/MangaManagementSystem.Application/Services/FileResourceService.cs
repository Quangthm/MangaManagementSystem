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
        private readonly IUnitOfWork _unitOfWork;

        public FileResourceService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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

        public async Task<IEnumerable<FileResourceDto>> GetFileResourcesByIdsAsync(IEnumerable<Guid> ids)
        {
            var idSet = ids.ToHashSet();
            var entities = await _unitOfWork.FileResources.FindAsync(f => idSet.Contains(f.FileResourceId));
            return entities.Select(MapToDto);
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

            // CHECK ck_file_resource_deleted_pair requires deleted_at_utc and
            // deleted_by_user_id to be set together. Saving deleted_at with a null
            // deleted_by violates it ("error saving the entity changes") — which was the
            // cause of the failing "delete page version" action.
            if (deletedByUserId is null || deletedByUserId == Guid.Empty)
            {
                throw new InvalidOperationException("A valid signed-in user is required to delete a file resource.");
            }

            entity.DeletedAtUtc = DateTime.UtcNow;
            entity.DeletedByUserId = deletedByUserId;
            _unitOfWork.FileResources.Update(entity);
            await _unitOfWork.SaveChangesAsync();
            return true;
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
            f.DeletedByUserId
        );
    }
}
