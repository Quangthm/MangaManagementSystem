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
            var versions = await _unitOfWork.ChapterPageVersions.FindAsync(v => v.ChapterPageId == chapterPageId);
            return versions
                .OrderBy(v => v.VersionNo)
                .Select(MapToDto);
        }

        public async Task<IEnumerable<ChapterPageVersionDto>> GetChapterPageVersionsByPageIdsAsync(IEnumerable<Guid> chapterPageIds)
        {
            var idSet = chapterPageIds.ToHashSet();
            var versions = await _unitOfWork.ChapterPageVersions.FindAsync(v => idSet.Contains(v.ChapterPageId));
            return versions
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

            // FK fk_page_region_version has no cascade, so a version with regions could not
            // be deleted ("error saving the entity changes"). Remove the version's regions
            // first with a set-based delete so this stays fast even for pages that
            // accumulated a very large number of regions.
            await _unitOfWork.PageRegions.ExecuteDeleteAsync(r => r.ChapterPageVersionId == id);

            _unitOfWork.ChapterPageVersions.Delete(entity);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<ChapterPageVersionDto> CreateVersionWithFileAndRegionsAsync(
            Guid chapterPageId,
            short versionNo,
            CreateFileResourceDto fileDto,
            string? versionNote,
            IEnumerable<CreatePageRegionDto> regionDtos,
            bool setAsCurrent,
            CancellationToken cancellationToken = default)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // 1. FileResource (metadata for the already-uploaded Cloudinary file).
                var file = new FileResource
                {
                    FilePurposeCode = fileDto.FilePurposeCode,
                    OriginalFileName = fileDto.OriginalFileName,
                    CloudinaryPublicId = fileDto.CloudinaryPublicId,
                    CloudinarySecureUrl = fileDto.CloudinarySecureUrl,
                    ContentType = fileDto.ContentType,
                    FileSizeBytes = fileDto.FileSizeBytes,
                    Sha256Hash = fileDto.Sha256Hash,
                    UploadedByUserId = fileDto.UploadedByUserId,
                    UploadedAtUtc = DateTime.UtcNow
                };
                await _unitOfWork.FileResources.AddAsync(file);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 2. The version row.
                var version = new ChapterPageVersion
                {
                    ChapterPageId = chapterPageId,
                    VersionNo = versionNo,
                    PageFileId = file.FileResourceId,
                    VersionNote = versionNote,
                    IsCurrentVersion = false
                };
                await _unitOfWork.ChapterPageVersions.AddAsync(version);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 3. Regions for the new version.
                foreach (var dto in regionDtos ?? Enumerable.Empty<CreatePageRegionDto>())
                {
                    var region = new PageRegion
                    {
                        ChapterPageVersionId = version.ChapterPageVersionId,
                        TypeCode = dto.TypeCode,
                        RegionLabel = dto.RegionLabel,
                        X = dto.X,
                        Y = dto.Y,
                        Width = dto.Width,
                        Height = dto.Height,
                        ConfidenceScore = dto.ConfidenceScore,
                        SourceType = dto.SourceType,
                        OriginalText = dto.OriginalText,
                        CreatedAtUtc = DateTime.UtcNow
                    };
                    await _unitOfWork.PageRegions.AddAsync(region);
                }
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 4. Optionally make it the current version (two-pass to satisfy the
                //    filtered unique index ux_chapter_page_version_current).
                if (setAsCurrent)
                {
                    var current = await _unitOfWork.ChapterPageVersions.FindAsync(
                        v => v.ChapterPageId == chapterPageId && v.IsCurrentVersion);
                    foreach (var v in current)
                    {
                        v.IsCurrentVersion = false;
                        _unitOfWork.ChapterPageVersions.Update(v);
                    }
                    await _unitOfWork.SaveChangesAsync(cancellationToken);

                    version.IsCurrentVersion = true;
                    _unitOfWork.ChapterPageVersions.Update(version);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }

                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return MapToDto(version);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        public async Task<bool> SetCurrentVersionAsync(Guid chapterPageId, Guid chapterPageVersionId)
        {
            var versions = await _unitOfWork.ChapterPageVersions.FindAsync(v => v.ChapterPageId == chapterPageId);
            var allVersions = versions.ToList();

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
