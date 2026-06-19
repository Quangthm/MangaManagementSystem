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
    }
}
