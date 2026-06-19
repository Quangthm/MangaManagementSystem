using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Editor;

namespace MangaManagementSystem.Web.Services.Api
{
    /// <summary>
    /// Typed client for the Tantou Editor Chapter Review Queue. Keeps the Razor layer free
    /// of Application/Infrastructure/EF access.
    /// </summary>
    public interface IEditorChapterReviewApiClient
    {
        /// <summary>
        /// Loads the chapter review queue (KPI counts + filtered chapter list). Optional
        /// status filter (e.g. "UNDER_REVIEW", "all"). Sends the transitional X-Actor-User-Id
        /// header.
        /// </summary>
        Task<EditorChapterReviewQueueDto> GetReviewQueueAsync(
            Guid actorUserId,
            string? statusFilter = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads the scoped review detail for one chapter. Returns a result that distinguishes
        /// "access denied" (HTTP 403) from success so the UI can show a friendly access-denied
        /// state without leaking chapter details.
        /// </summary>
        Task<EditorChapterReviewDetailResult> GetReviewDetailAsync(
            Guid actorUserId,
            Guid chapterId,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Outcome of a chapter review detail request. Exactly one of <see cref="Detail"/> (success)
    /// or the <see cref="AccessDenied"/> flag is meaningful.
    /// </summary>
    public sealed record EditorChapterReviewDetailResult(
        EditorChapterReviewDetailDto? Detail,
        bool AccessDenied,
        string? ErrorMessage)
    {
        public bool Succeeded => Detail is not null;

        public static EditorChapterReviewDetailResult Success(EditorChapterReviewDetailDto detail) =>
            new(detail, false, null);

        public static EditorChapterReviewDetailResult Forbidden(string message) =>
            new(null, true, message);

        public static EditorChapterReviewDetailResult Failure(string message) =>
            new(null, false, message);
    }
}
