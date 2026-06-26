using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface IQuickSelectService
    {
        Task<IReadOnlyList<QuickSelectChapterDto>> GetQuickSelectChaptersAsync(
            Guid actorUserId,
            Guid seriesId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<QuickSelectPageDto>> GetQuickSelectPagesAsync(
            Guid chapterId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<QuickSelectAssistantDto>> GetQuickSelectAssistantsAsync(
            Guid actorUserId,
            Guid seriesId,
            CancellationToken cancellationToken = default);

        Task<QuickSelectTaskAssignmentResult> AssignQuickSelectTasksAsync(
            Guid actorUserId,
            QuickSelectTaskAssignmentRequest request,
            CancellationToken cancellationToken = default);
    }
}
