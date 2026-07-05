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

        public Task<bool> DeleteChapterPageVersionAsync(Guid id) =>
            // BR-CP-013 / BR-CP-020 / BR-REG-012: page versions and their regions are kept for
            // traceability and must never be hard-deleted. Removing a version row (and its regions)
            // would also orphan any linked tasks/annotations. Use DeleteVersionImageAsync to remove
            // only the image while preserving the version as a history placeholder.
            throw new InvalidOperationException(
                "Page versions cannot be deleted. Delete the version image instead (DeleteVersionImageAsync).");

        public async Task<DeleteVersionImageResultDto> DeleteVersionImageAsync(
            Guid versionId,
            Guid actorUserId,
            string? actorRoleName,
            CancellationToken cancellationToken = default)
        {
            if (actorUserId == Guid.Empty)
            {
                return new DeleteVersionImageResultDto(false, "A valid signed-in user is required to delete the image.", null);
            }

            var version = await _unitOfWork.ChapterPageVersions.GetByIdAsync(versionId);
            if (version == null)
            {
                return new DeleteVersionImageResultDto(false, "Version not found.", null);
            }

            // Guard (BR-ANN-017 / BR-PGTASK / BR-FILE-003): tasks and annotations are version-scoped
            // (attached to THIS version's regions), so block deleting the image only when THIS version
            // has an unresolved annotation or an active task on its own regions. This matches the
            // version-scoped Task Panel: what you see on the version is exactly what protects it.
            var regions = await _unitOfWork.PageRegions.FindAsync(r => r.ChapterPageVersionId == versionId);
            var regionIds = regions.Select(r => r.PageRegionId).ToHashSet();
            if (regionIds.Count > 0)
            {
                var annotations = await _unitOfWork.ChapterPageAnnotations.GetByPageRegionIdsAsync(regionIds.ToList());
                if (annotations.Any(a => a.ResolvedAtUtc == null))
                {
                    return new DeleteVersionImageResultDto(false,
                        "This version has unresolved annotations. Resolve them before deleting its image.", null);
                }

                var tasks = await _unitOfWork.ChapterPageTasks.GetByChapterPageIdWithRegionsAsync(version.ChapterPageId);
                var hasActiveTask = tasks.Any(t =>
                    (t.StatusCode == "ASSIGNED" || t.StatusCode == "UNDER_REVIEW") &&
                    t.PageRegions.Any(r => regionIds.Contains(r.PageRegionId)));
                if (hasActiveTask)
                {
                    return new DeleteVersionImageResultDto(false,
                        "This version is referenced by an active assistant task. Complete or cancel it before deleting its image.", null);
                }
            }

            // Soft-delete only the FileResource (keep the version row + regions as a history
            // placeholder per BR-CP-013/020 and BR-FILE-008). Setting both deleted_at_utc and
            // deleted_by_user_id satisfies ck_file_resource_deleted_pair. Uploading again creates
            // the next version number rather than reviving this one.
            string? publicId = null;
            var file = await _unitOfWork.FileResources.GetByIdAsync(version.PageFileId);
            if (file != null && file.DeletedAtUtc == null)
            {
                file.DeletedAtUtc = DateTime.UtcNow;
                file.DeletedByUserId = actorUserId;
                _unitOfWork.FileResources.Update(file);
                publicId = file.CloudinaryPublicId;
            }

            await _unitOfWork.AuditEvents.AddAsync(new AuditEvent
            {
                OccurredAtUtc = DateTime.UtcNow,
                ActorUserId = actorUserId,
                ActorRoleName = actorRoleName,
                ActionCode = "VERSION_IMAGE_DELETED",
                EntityType = "ChapterPageVersion",
                EntityId = versionId.ToString(),
                DetailJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    version_no = version.VersionNo,
                    file_resource_id = version.PageFileId
                })
            });

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new DeleteVersionImageResultDto(true, null, publicId);
        }

        public async Task<ChapterPageVersionDto> CreateVersionWithFileAndRegionsAsync(
            Guid chapterPageId,
            short versionNo,
            CreateFileResourceDto fileDto,
            string? versionNote,
            IEnumerable<CreatePageRegionDto> regionDtos,
            bool setAsCurrent,
            Guid actorUserId,
            string? actorRoleName,
            CancellationToken cancellationToken = default)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // 1. FileResource (metadata for the already-uploaded Cloudinary file). The uploader is
                //    taken from the trusted signed-in actor (BR-CP-008), not the client-supplied value.
                var file = new FileResource
                {
                    FilePurposeCode = fileDto.FilePurposeCode,
                    OriginalFileName = fileDto.OriginalFileName,
                    CloudinaryPublicId = fileDto.CloudinaryPublicId,
                    CloudinarySecureUrl = fileDto.CloudinarySecureUrl,
                    ContentType = fileDto.ContentType,
                    FileSizeBytes = fileDto.FileSizeBytes,
                    Sha256Hash = fileDto.Sha256Hash,
                    UploadedByUserId = actorUserId,
                    UploadedAtUtc = DateTime.UtcNow
                };
                await _unitOfWork.FileResources.AddAsync(file);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 2. The version row. The version number is computed server-side as max(existing) + 1
                //    (BR-CP-009/010/011) so a stale client value cannot create a duplicate or gap.
                var existing = await _unitOfWork.ChapterPageVersions.FindAsync(v => v.ChapterPageId == chapterPageId);
                var nextVersionNo = (short)(existing.Select(v => (int)v.VersionNo).DefaultIfEmpty(0).Max() + 1);
                var version = new ChapterPageVersion
                {
                    ChapterPageId = chapterPageId,
                    VersionNo = nextVersionNo,
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

                // 5. Audit (Command = write + audit): record the new version creation.
                await _unitOfWork.AuditEvents.AddAsync(new AuditEvent
                {
                    OccurredAtUtc = DateTime.UtcNow,
                    ActorUserId = actorUserId,
                    ActorRoleName = actorRoleName,
                    ActionCode = "VERSION_CREATED",
                    EntityType = "ChapterPageVersion",
                    EntityId = version.ChapterPageVersionId.ToString(),
                    DetailJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        chapter_page_id = chapterPageId,
                        version_no = version.VersionNo,
                        file_resource_id = file.FileResourceId,
                        set_as_current = setAsCurrent
                    })
                });
                await _unitOfWork.SaveChangesAsync(cancellationToken);

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

            var newCurrent = allVersions.FirstOrDefault(v => v.ChapterPageVersionId == chapterPageVersionId);
            if (newCurrent == null)
            {
                return false;
            }

            // Both passes run in one transaction so the page is never left without a current version
            // (BR-CP-012) if the second write fails. The two passes are still needed to satisfy the
            // filtered unique index ux_chapter_page_version_current.
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // First pass: unset the previous current version to clear the unique constraint.
                foreach (var version in allVersions.Where(v => v.IsCurrentVersion && v.ChapterPageVersionId != chapterPageVersionId))
                {
                    version.IsCurrentVersion = false;
                    _unitOfWork.ChapterPageVersions.Update(version);
                }
                await _unitOfWork.SaveChangesAsync();

                // Second pass: set the new current version.
                newCurrent.IsCurrentVersion = true;
                _unitOfWork.ChapterPageVersions.Update(newCurrent);
                await _unitOfWork.SaveChangesAsync();

                await _unitOfWork.CommitTransactionAsync();
                return true;
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<CreatePageWithVersionResponseDto> CreatePageWithVersionAndFileAsync(
            CreatePageWithVersionRequestDto request,
            Guid actorUserId,
            string? actorRoleName,
            CancellationToken cancellationToken = default)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // The uploader is taken from the trusted signed-in actor (BR-CP-008), not the
                // client-supplied file DTO value.
                var file = new FileResource
                {
                    FilePurposeCode = request.FileDto.FilePurposeCode,
                    OriginalFileName = request.FileDto.OriginalFileName,
                    CloudinaryPublicId = request.FileDto.CloudinaryPublicId,
                    CloudinarySecureUrl = request.FileDto.CloudinarySecureUrl,
                    ContentType = request.FileDto.ContentType,
                    FileSizeBytes = request.FileDto.FileSizeBytes,
                    Sha256Hash = request.FileDto.Sha256Hash,
                    UploadedByUserId = actorUserId,
                    UploadedAtUtc = DateTime.UtcNow
                };
                await _unitOfWork.FileResources.AddAsync(file);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var page = new ChapterPage
                {
                    ChapterId = request.ChapterId,
                    PageNo = request.PageNo,
                    PageNotes = request.PageNotes
                };
                await _unitOfWork.ChapterPages.AddAsync(page);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                var version = new ChapterPageVersion
                {
                    ChapterPageId = page.ChapterPageId,
                    VersionNo = 1,
                    PageFileId = file.FileResourceId,
                    VersionNote = request.VersionNote,
                    IsCurrentVersion = true
                };
                await _unitOfWork.ChapterPageVersions.AddAsync(version);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Audit (Command = write + audit): record the new page + its first version.
                await _unitOfWork.AuditEvents.AddAsync(new AuditEvent
                {
                    OccurredAtUtc = DateTime.UtcNow,
                    ActorUserId = actorUserId,
                    ActorRoleName = actorRoleName,
                    ActionCode = "PAGE_CREATED",
                    EntityType = "ChapterPage",
                    EntityId = page.ChapterPageId.ToString(),
                    DetailJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        chapter_id = page.ChapterId,
                        page_no = page.PageNo,
                        chapter_page_version_id = version.ChapterPageVersionId,
                        file_resource_id = file.FileResourceId
                    })
                });
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return new CreatePageWithVersionResponseDto(
                    new ChapterPageDto(page.ChapterPageId, page.ChapterId, page.PageNo, page.PageNotes, null, null),
                    MapToDto(version),
                    new FileResourceDto(
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
                        file.DeletedByUserId)
                );
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
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

