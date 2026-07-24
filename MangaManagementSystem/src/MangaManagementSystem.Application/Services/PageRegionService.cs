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
    public class PageRegionService : IPageRegionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IImageMetadataProvider _imageMetadataProvider;

        public PageRegionService(IUnitOfWork unitOfWork, IImageMetadataProvider imageMetadataProvider)
        {
            _unitOfWork = unitOfWork;
            _imageMetadataProvider = imageMetadataProvider;
        }

        public async Task<PageRegionDto> CreatePageRegionAsync(CreatePageRegionDto dto)
        {
            var entity = new PageRegion
            {
                ChapterPageVersionId = dto.ChapterPageVersionId,
                TypeCode = dto.TypeCode,
                RegionLabel = dto.RegionLabel,
                X = dto.X,
                Y = dto.Y,
                Width = dto.Width,
                Height = dto.Height,
                ConfidenceScore = dto.ConfidenceScore,
                SourceType = dto.SourceType,
                OriginalText = dto.OriginalText,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = dto.CreatedByUserId
            };
            await _unitOfWork.PageRegions.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<PageRegionDto?> GetPageRegionByIdAsync(Guid id)
        {
            var entity = await _unitOfWork.PageRegions.GetByIdAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<IEnumerable<PageRegionDto>> GetPageRegionsByChapterPageVersionIdAsync(Guid chapterPageVersionId)
        {
            var regions = await _unitOfWork.PageRegions.FindAsync(r => r.ChapterPageVersionId == chapterPageVersionId);
            return regions.Select(MapToDto);
        }

        public async Task<IEnumerable<PageRegionDto>> GetPageRegionsByVersionIdsAsync(IEnumerable<Guid> chapterPageVersionIds)
        {
            var idSet = chapterPageVersionIds.ToHashSet();
            if (idSet.Count == 0) return Enumerable.Empty<PageRegionDto>();
            var regions = await _unitOfWork.PageRegions.FindAsync(r => idSet.Contains(r.ChapterPageVersionId));
            return regions.Select(MapToDto);
        }

        public async Task<Dictionary<Guid, int>> GetRegionCountsByVersionIdsAsync(IEnumerable<Guid> chapterPageVersionIds)
        {
            var idSet = chapterPageVersionIds.ToHashSet();
            if (idSet.Count == 0) return new Dictionary<Guid, int>();
            return await _unitOfWork.PageRegions.CountByAsync(
                r => idSet.Contains(r.ChapterPageVersionId),
                r => r.ChapterPageVersionId);
        }

        public async Task<PageRegionDto?> UpdatePageRegionAsync(UpdatePageRegionDto dto)
        {
            var entity = await _unitOfWork.PageRegions.GetByIdAsync(dto.PageRegionId);
            if (entity == null)
            {
                return null;
            }

            entity.ChapterPageVersionId = dto.ChapterPageVersionId;
            entity.TypeCode = dto.TypeCode;
            entity.RegionLabel = dto.RegionLabel;
            entity.X = dto.X;
            entity.Y = dto.Y;
            entity.Width = dto.Width;
            entity.Height = dto.Height;
            entity.ConfidenceScore = dto.ConfidenceScore;
            entity.SourceType = dto.SourceType;
            entity.OriginalText = dto.OriginalText;
            entity.UpdatedAtUtc = DateTime.UtcNow;
            _unitOfWork.PageRegions.Update(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<bool> DeletePageRegionAsync(Guid id)
        {
            var entity = await _unitOfWork.PageRegions.GetByIdAsync(id);
            if (entity == null)
            {
                return false;
            }

            // BR-ANN-017 / BR-PGTASK: a region still referenced by an annotation or a page task must
            // NOT be deleted — doing so would orphan open feedback / assigned work (and hit the
            // no-cascade FKs). Enforce this at the Application layer, not only in the UI.
            var linked = await GetLinkedRegionIdsAsync(entity.ChapterPageVersionId, new List<Guid> { id });
            if (linked.Contains(id))
            {
                throw new InvalidOperationException(
                    "This region cannot be deleted because it is already used by a task or annotation.");
            }

            _unitOfWork.PageRegions.Delete(entity);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Returns, for the given candidate region ids on a version, the subset still referenced by an
        /// annotation or a page task (BR-ANN-017 / BR-PGTASK). Shared by single-delete and bulk-replace.
        /// </summary>
        private async Task<HashSet<Guid>> GetLinkedRegionIdsAsync(Guid chapterPageVersionId, IReadOnlyCollection<Guid> candidateIds)
        {
            var protectedIds = new HashSet<Guid>();
            if (candidateIds.Count == 0)
            {
                return protectedIds;
            }

            var candidateList = candidateIds.ToList();

            var linkedAnnotations = await _unitOfWork.ChapterPageAnnotations.GetByPageRegionIdsAsync(candidateList);
            foreach (var annotationRegionId in linkedAnnotations
                         .SelectMany(a => a.PageRegions.Select(pr => pr.PageRegionId)))
            {
                if (candidateIds.Contains(annotationRegionId))
                {
                    protectedIds.Add(annotationRegionId);
                }
            }

            var version = await _unitOfWork.ChapterPageVersions.GetByIdAsync(chapterPageVersionId);
            if (version != null)
            {
                var tasks = await _unitOfWork.ChapterPageTasks.GetByChapterPageIdWithRegionsAsync(version.ChapterPageId);
                foreach (var taskRegionId in tasks.SelectMany(t => t.PageRegions.Select(pr => pr.PageRegionId)))
                {
                    if (candidateIds.Contains(taskRegionId))
                    {
                        protectedIds.Add(taskRegionId);
                    }
                }
            }

            return protectedIds;
        }

        /// <summary>
        /// BR-REG-031 / #8 full-page default: returns the existing FULL_PAGE region for the version, or
        /// creates one covering the whole page. Image dimensions are resolved from Cloudinary metadata
        /// (BR-REG-032) — no DB transaction is held during the lookup. Used when a task/annotation is
        /// created without an explicit region selection.
        /// </summary>
        public async Task<PageRegionDto> EnsureFullPageRegionAsync(
            Guid chapterPageVersionId,
            Guid actorUserId,
            CancellationToken cancellationToken = default)
        {
            // 1. Reuse an existing whole-page region if one already exists for this version.
            var existing = await _unitOfWork.PageRegions.FindAsync(
                r => r.ChapterPageVersionId == chapterPageVersionId && r.TypeCode == "FULL_PAGE");
            var reuse = existing.FirstOrDefault();
            if (reuse != null)
            {
                return MapToDto(reuse);
            }

            // 2. Resolve the page image dimensions from Cloudinary (FileResource stores no width/height).
            var version = await _unitOfWork.ChapterPageVersions.GetByIdAsync(chapterPageVersionId)
                ?? throw new InvalidOperationException("The page version could not be found.");
            var file = await _unitOfWork.FileResources.GetByIdAsync(version.PageFileId)
                ?? throw new InvalidOperationException("The page image could not be found.");

            var bounds = await _imageMetadataProvider.GetImageBoundsAsync(file.CloudinaryPublicId, cancellationToken)
                ?? throw new InvalidOperationException("Could not resolve the page image dimensions for a full-page region.");

            // 3. Create the FULL_PAGE region (x=0, y=0, full width/height — ck_page_region_full_page_shape).
            var region = new PageRegion
            {
                ChapterPageVersionId = chapterPageVersionId,
                TypeCode = "FULL_PAGE",
                RegionLabel = "Full page",
                X = 0,
                Y = 0,
                Width = bounds.Width,
                Height = bounds.Height,
                ConfidenceScore = null,
                SourceType = "MANUAL",
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = actorUserId
            };
            await _unitOfWork.PageRegions.AddAsync(region);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return MapToDto(region);
        }

        public async Task<bool> BulkReplacePageRegionsAsync(Guid chapterPageVersionId, IEnumerable<CreatePageRegionDto> dtos)
        {
            // Get all existing regions for this version
            var regions = await _unitOfWork.PageRegions.FindAsync(r => r.ChapterPageVersionId == chapterPageVersionId);
            var existing = regions.ToList();

            // Exclude pin regions (small bounding boxes used for pins on annotations/tasks) from bulk replacement
            existing.RemoveAll(r => r.Width <= 0.05m && r.Height <= 0.05m);

            // Create or update
            foreach (var dto in dtos)
            {
                PageRegion? existingRegion = null;
                if (dto.PageRegionId.HasValue && dto.PageRegionId.Value != Guid.Empty)
                {
                    existingRegion = existing.FirstOrDefault(r => r.PageRegionId == dto.PageRegionId.Value);
                }
                if (existingRegion == null && !string.IsNullOrEmpty(dto.RegionLabel))
                {
                    existingRegion = existing.FirstOrDefault(r => r.RegionLabel == dto.RegionLabel);
                }

                if (existingRegion != null)
                {
                    bool isChanged = existingRegion.TypeCode != dto.TypeCode ||
                                     existingRegion.X != dto.X ||
                                     existingRegion.Y != dto.Y ||
                                     existingRegion.Width != dto.Width ||
                                     existingRegion.Height != dto.Height ||
                                     existingRegion.ConfidenceScore != dto.ConfidenceScore ||
                                     existingRegion.SourceType != dto.SourceType ||
                                     existingRegion.OriginalText != dto.OriginalText ||
                                     existingRegion.RegionLabel != dto.RegionLabel;

                    if (isChanged)
                    {
                        existingRegion.TypeCode = dto.TypeCode;
                        existingRegion.X = dto.X;
                        existingRegion.Y = dto.Y;
                        existingRegion.Width = dto.Width;
                        existingRegion.Height = dto.Height;
                        existingRegion.ConfidenceScore = dto.ConfidenceScore;
                        existingRegion.SourceType = dto.SourceType;
                        existingRegion.OriginalText = dto.OriginalText;
                        existingRegion.RegionLabel = dto.RegionLabel;
                        // Record WHO actually made this change (the acting user for THIS request, e.g. a
                        // Tantou Editor editing a Mangaka-created region during review) — not the original
                        // creator, which is what this used to copy. dto.CreatedByUserId carries the acting
                        // user id. Set the matching timestamp so updated_by/updated_at stay a consistent pair.
                        if (dto.CreatedByUserId.HasValue && dto.CreatedByUserId.Value != Guid.Empty)
                        {
                            existingRegion.UpdatedByUserId = dto.CreatedByUserId;
                            existingRegion.UpdatedAtUtc = DateTime.UtcNow;
                        }
                        else
                        {
                            existingRegion.UpdatedByUserId = null;
                            existingRegion.UpdatedAtUtc = null;
                        }
                        _unitOfWork.PageRegions.Update(existingRegion);
                    }
                    existing.Remove(existingRegion);
                }
                else
                {
                    // Create new
                    var entity = new PageRegion
                    {
                        ChapterPageVersionId = chapterPageVersionId,
                        TypeCode = dto.TypeCode,
                        RegionLabel = dto.RegionLabel,
                        X = dto.X,
                        Y = dto.Y,
                        Width = dto.Width,
                        Height = dto.Height,
                        ConfidenceScore = dto.ConfidenceScore,
                        SourceType = dto.SourceType,
                        OriginalText = dto.OriginalText,
                        CreatedAtUtc = DateTime.UtcNow,
                        CreatedByUserId = dto.CreatedByUserId
                    };
                    await _unitOfWork.PageRegions.AddAsync(entity);
                }
            }

            // Delete remaining (regions the user removed locally and that are NOT in the new dtos).
            // BR-ANN-017 / BR-PGTASK: a region that is still referenced by an annotation or a page
            // task must NOT be deleted — doing so would orphan open feedback / assigned work (and
            // hits the no-cascade FKs). Such regions are kept even if their box was removed locally.
            if (existing.Count > 0)
            {
                var candidateIds = existing.Select(r => r.PageRegionId).ToList();

                var linkedAnnotations = await _unitOfWork.ChapterPageAnnotations.GetByPageRegionIdsAsync(candidateIds);
                var protectedIds = linkedAnnotations
                    .SelectMany(a => a.PageRegions.Select(pr => pr.PageRegionId))
                    .ToHashSet();

                var version = await _unitOfWork.ChapterPageVersions.GetByIdAsync(chapterPageVersionId);
                if (version != null)
                {
                    var tasks = await _unitOfWork.ChapterPageTasks.GetByChapterPageIdWithRegionsAsync(version.ChapterPageId);
                    foreach (var taskRegionId in tasks.SelectMany(t => t.PageRegions.Select(pr => pr.PageRegionId)))
                    {
                        protectedIds.Add(taskRegionId);
                    }
                }

                foreach (var r in existing)
                {
                    if (protectedIds.Contains(r.PageRegionId))
                    {
                        continue; // keep: still linked to an annotation or task
                    }
                    _unitOfWork.PageRegions.Delete(r);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static PageRegionDto MapToDto(PageRegion r) => new(
            r.PageRegionId,
            r.ChapterPageVersionId,
            r.TypeCode,
            r.RegionLabel,
            r.X,
            r.Y,
            r.Width,
            r.Height,
            r.ConfidenceScore,
            r.SourceType,
            r.OriginalText,
            r.CreatedByUserId,
            r.UpdatedByUserId
        );
    }
}
