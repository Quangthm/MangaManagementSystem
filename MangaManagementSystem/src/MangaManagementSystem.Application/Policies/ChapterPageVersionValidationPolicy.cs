using MangaManagementSystem.Domain.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Policies
{
    public interface IChapterPageVersionValidationPolicy
    {
        Task<(bool IsAllowed, string? ErrorMessage)> CanDeleteVersionImageAsync(
            Guid chapterPageVersionId, 
            CancellationToken cancellationToken = default);
    }

    public class ChapterPageVersionValidationPolicy : IChapterPageVersionValidationPolicy
    {
        private readonly IUnitOfWork _unitOfWork;

        public ChapterPageVersionValidationPolicy(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<(bool IsAllowed, string? ErrorMessage)> CanDeleteVersionImageAsync(
            Guid chapterPageVersionId, 
            CancellationToken cancellationToken = default)
        {
            var version = await _unitOfWork.ChapterPageVersions.GetByIdAsync(chapterPageVersionId);
            if (version == null)
            {
                return (false, "Version not found.");
            }

            // Guard (BR-ANN-017 / BR-PGTASK / BR-FILE-003): tasks and annotations are version-scoped
            // (attached to THIS version's regions), so block deleting the image only when THIS version
            // has an unresolved annotation or an active task on its own regions. This matches the
            // version-scoped Task Panel: what you see on the version is exactly what protects it.
            var regions = await _unitOfWork.PageRegions.FindAsync(r => r.ChapterPageVersionId == chapterPageVersionId);
            var regionIds = regions.Select(r => r.PageRegionId).ToHashSet();
            
            if (regionIds.Count > 0)
            {
                var annotations = await _unitOfWork.ChapterPageAnnotations.GetByPageRegionIdsAsync(regionIds.ToList());
                if (annotations.Any(a => a.ResolvedAtUtc == null))
                {
                    return (false, "This version has unresolved annotations. Resolve them before deleting its image.");
                }

                var tasks = await _unitOfWork.ChapterPageTasks.GetByChapterPageIdWithRegionsAsync(version.ChapterPageId);
                var hasActiveTask = tasks.Any(t =>
                    (t.StatusCode == "ASSIGNED" || t.StatusCode == "UNDER_REVIEW") &&
                    t.PageRegions.Any(r => regionIds.Contains(r.PageRegionId)));
                if (hasActiveTask)
                {
                    return (false, "This version is referenced by an active assistant task. Complete or cancel it before deleting its image.");
                }
            }

            return (true, null);
        }
    }
}
