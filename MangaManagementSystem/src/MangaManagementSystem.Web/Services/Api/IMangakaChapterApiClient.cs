using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;

namespace MangaManagementSystem.Web.Services.Api
{
    /// <summary>
    /// Typed Web-to-API client for Mangaka chapter draft and submission management.
    /// Keeps Razor components free of direct HttpClient concerns.
    /// </summary>
    public interface IMangakaChapterApiClient
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
            CreateChapterDraftRequest request,
            CancellationToken cancellationToken = default);

        Task<MangakaChapterListItemDto> UpdateChapterDraftAsync(
            Guid actorUserId,
            Guid chapterId,
            UpdateChapterDraftRequest request,
            CancellationToken cancellationToken = default);

        Task<MangakaChapterListItemDto> SubmitChapterForReviewAsync(
            Guid actorUserId,
            Guid chapterId,
            CancellationToken cancellationToken = default);

        Task<MangakaChapterListItemDto> CancelChapterSubmissionAsync(
            Guid actorUserId,
            Guid chapterId,
            CancellationToken cancellationToken = default);

        Task<MangakaChapterListItemDto> CancelChapterAsync(
            Guid actorUserId,
            Guid chapterId,
            CancellationToken cancellationToken = default);

        Task<SetChapterPlannedReleaseDateResponse> SetPlannedReleaseDateAsync(
            Guid actorUserId,
            Guid chapterId,
            SetPlannedReleaseDateRequest request,
            CancellationToken cancellationToken = default);
    }
}
