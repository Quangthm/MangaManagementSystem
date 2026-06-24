using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Services
{
    public class ChapterPageVersionService : IChapterPageVersionService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ChapterPageVersionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ChapterPageVersionDto> CreateChapterPageVersionAsync(CreateChapterPageVersionDto dto)
        {
            var entity = new ChapterPageVersion
            {
                ChapterPageId = dto.ChapterPageId,
                VersionNo = dto.VersionNo,
                PageFileId = dto.PageFileId,
                VersionNote = dto.VersionNote,
                IsCurrentVersion = false
            };
            await _unitOfWork.ChapterPageVersions.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<ChapterPageVersionDto?> GetChapterPageVersionByIdAsync(Guid id)
        {
            var entity = await _unitOfWork.ChapterPageVersions.GetByIdAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<IEnumerable<ChapterPageVersionDto>> GetChapterPageVersionsByChapterPageIdAsync(Guid chapterPageId)
        {
            var all = await _unitOfWork.ChapterPageVersions.GetAllAsync();
            return all
                .Where(v => v.ChapterPageId == chapterPageId)
                .OrderBy(v => v.VersionNo)
                .Select(MapToDto);
        }

        public async Task<ChapterPageVersionDto?> UpdateChapterPageVersionAsync(UpdateChapterPageVersionDto dto)
        {
            var entity = await _unitOfWork.ChapterPageVersions.GetByIdAsync(dto.ChapterPageVersionId);
            if (entity == null)
            {
                return null;
            }

            entity.ChapterPageId = dto.ChapterPageId;
            entity.VersionNo = dto.VersionNo;
            entity.PageFileId = dto.PageFileId;
            entity.VersionNote = dto.VersionNote;
            entity.IsCurrentVersion = dto.IsCurrentVersion;
            _unitOfWork.ChapterPageVersions.Update(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<bool> DeleteChapterPageVersionAsync(Guid id)
        {
            var entity = await _unitOfWork.ChapterPageVersions.GetByIdAsync(id);
            if (entity == null)
            {
                return false;
            }

            _unitOfWork.ChapterPageVersions.Delete(entity);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetCurrentVersionAsync(Guid chapterPageId, Guid chapterPageVersionId)
        {
            var all = await _unitOfWork.ChapterPageVersions.GetAllAsync();
            var allVersions = all.Where(v => v.ChapterPageId == chapterPageId).ToList();

            // First pass: unset the current version to clear the unique constraint
            foreach (var version in allVersions.Where(v => v.IsCurrentVersion && v.ChapterPageVersionId != chapterPageVersionId))
            {
                version.IsCurrentVersion = false;
                _unitOfWork.ChapterPageVersions.Update(version);
            }
            await _unitOfWork.SaveChangesAsync();

            // Second pass: set the new current version
            var newCurrent = allVersions.FirstOrDefault(v => v.ChapterPageVersionId == chapterPageVersionId);
            if (newCurrent != null)
            {
                newCurrent.IsCurrentVersion = true;
                _unitOfWork.ChapterPageVersions.Update(newCurrent);
                await _unitOfWork.SaveChangesAsync();
            }

            return true;
        }

        private static ChapterPageVersionDto MapToDto(ChapterPageVersion v) => new(
            v.ChapterPageVersionId,
            v.ChapterPageId,
            v.VersionNo,
            v.PageFileId,
            v.VersionNote,
            v.IsCurrentVersion
        );
    }
}
