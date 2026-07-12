using MangaManagementSystem.Application.Common;
using MangaManagementSystem.Application.DTOs.Admin;

namespace MangaManagementSystem.Web.Services.Api
{
    public interface IAdminFileApiClient
    {
        Task<AdminFilePageDto> SearchAsync(
            string? search = null,
            string? filePurposeCode = null,
            string? deletedState = AdminFileDeletionStates.Active,
            DateTime? fromUtc = null,
            DateTime? toUtc = null,
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default);

        Task<AdminFileDetailDto?> GetByIdAsync(
            Guid fileResourceId,
            CancellationToken cancellationToken = default);

        Task<AdminFileDetailDto> DeleteAsync(
            Guid fileResourceId,
            string deleteReason,
            CancellationToken cancellationToken = default);

        Task<AdminFileDetailDto> SoftDeleteAsync(
            Guid fileResourceId,
            string deleteReason,
            CancellationToken cancellationToken = default);

        Task<AdminFileCleanupResultDto> CleanupAsync(
            Guid fileResourceId,
            CancellationToken cancellationToken = default);

        Task<AdminFileCleanupBatchResultDto> CleanupDeletedAsync(
            int batchSize = 20,
            CancellationToken cancellationToken = default);

        Task<AdminFileContentResult> GetPreviewAsync(
            Guid fileResourceId,
            CancellationToken cancellationToken = default);

        Task<AdminFileContentResult> GetDownloadAsync(
            Guid fileResourceId,
            CancellationToken cancellationToken = default);
    }

    public sealed record AdminFileContentResult(
        byte[] Content,
        string ContentType,
        string FileName,
        bool IsPlaceholder,
        string? PlaceholderReason);
}
