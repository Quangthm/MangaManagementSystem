using MangaManagementSystem.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Domain.Interfaces
{
    public interface IChapterPageAnnotationRepository : IGenericRepository<ChapterPageAnnotation>
    {
        Task<Guid> CreateChapterPageAnnotationAsync(
            Guid actorUserId,
            IReadOnlyList<Guid> pageRegionIds,
            string issueTypeCode,
            string annotationText);

        Task<ChapterPageAnnotation?> GetByIdWithRegionsAsync(Guid id);

        Task<IReadOnlyList<ChapterPageAnnotation>> GetByPageRegionIdAsync(Guid pageRegionId);

        Task<IReadOnlyList<ChapterPageAnnotation>> GetByPageRegionIdsAsync(IReadOnlyList<Guid> pageRegionIds);

        Task<bool> ResolveAnnotationAsync(
            Guid actorUserId,
            Guid annotationId,
            string? resolutionNote = null);

        Task<IReadOnlyList<ChapterPageAnnotation>> GetByChapterPageIdWithRegionsAsync(Guid chapterPageId);
    }
}
