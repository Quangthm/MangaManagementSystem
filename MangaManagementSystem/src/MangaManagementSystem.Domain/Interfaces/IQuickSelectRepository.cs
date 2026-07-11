using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Domain.ReadModels;

namespace MangaManagementSystem.Domain.Interfaces
{
    public interface IQuickSelectRepository
    {
        Task<IReadOnlyList<QuickSelectChapterDto>> GetQuickSelectChaptersAsync(
            Guid seriesId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<QuickSelectPageDto>> GetQuickSelectPagesAsync(
            Guid chapterId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<QuickSelectAssistantDto>> GetQuickSelectAssistantsAsync(
            Guid seriesId,
            CancellationToken cancellationToken = default);

        Task<QuickSelectTaskAssignmentResult> PersistQuickSelectAssignmentAsync(
            QuickSelectAssignmentPlan plan,
            CancellationToken cancellationToken = default);
    }
}
