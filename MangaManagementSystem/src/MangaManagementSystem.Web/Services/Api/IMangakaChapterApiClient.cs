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
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<MangakaChapterListItemDto>> GetSeriesChaptersAsync(
            Guid seriesId,
            CancellationToken cancellationToken = default);

        Task<MangakaChapterListItemDto> CreateChapterDraftAsync(
            CreateChapterDraftRequest request,
            CancellationToken cancellationToken = default);

        Task<MangakaChapterListItemDto> UpdateChapterDraftAsync(
            Guid chapterId,
            UpdateChapterDraftRequest request,
            CancellationToken cancellationToken = default);

        Task<MangakaChapterListItemDto> SubmitChapterForReviewAsync(
            Guid chapterId,
            CancellationToken cancellationToken = default);

        Task<MangakaChapterListItemDto> CancelChapterSubmissionAsync(
            Guid chapterId,
            CancellationToken cancellationToken = default);

        Task<MangakaChapterListItemDto> CancelChapterAsync(
            Guid chapterId,
            CancellationToken cancellationToken = default);

        Task<SetChapterPlannedReleaseDateResponse> SetPlannedReleaseDateAsync(
            Guid chapterId,
            SetPlannedReleaseDateRequest request,
            CancellationToken cancellationToken = default);
    }
}
