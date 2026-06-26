using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;

namespace MangaManagementSystem.Application.Interfaces
{
    /// <summary>
    /// Repository interface for Mangaka chapter workflow operations.
    /// Lives in Application because it returns Application DTOs.
    /// </summary>
    public interface IMangakaChapterRepository
    {
        Task<IReadOnlyList<MangakaChapterListItemDto>> GetMyChaptersAsync(
            Guid actorUserId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<MangakaChapterListItemDto>> GetSeriesChaptersAsync(
            Guid actorUserId,
            Guid seriesId,
            CancellationToken cancellationToken = default);

        Task<MangakaChapterListItemDto> CreateChapterDraftAsync(
            Guid actorUserId,
            Guid seriesId,
            string chapterNumberLabel,
            string? chapterTitle,
            CancellationToken cancellationToken = default);

        Task<MangakaChapterListItemDto> UpdateChapterDraftAsync(
            Guid actorUserId,
            Guid chapterId,
            string chapterNumberLabel,
            string? chapterTitle,
            CancellationToken cancellationToken = default);

        Task<MangakaChapterListItemDto> SubmitChapterForReviewAsync(
            Guid actorUserId,
            Guid chapterId,
            CancellationToken cancellationToken = default);

        Task<MangakaChapterListItemDto> ScheduleApprovedChapterAsync(
            Guid actorUserId,
            Guid chapterId,
            DateTime plannedReleaseDate,
            CancellationToken cancellationToken = default);
    }
}
