using MangaManagementSystem.Application.DTOs.Admin;

namespace MangaManagementSystem.Web.Services.Api
{
    public interface IAdminAuditApiClient
    {
        Task<AdminAuditPageDto> SearchAsync(
            string? search = null,
            string? actionCode = null,
            string? entityType = null,
            DateTime? fromUtc = null,
            DateTime? toUtc = null,
            int pageNumber = 1,
            int pageSize = 20,
            CancellationToken cancellationToken = default);

        Task<AdminAuditFilterOptionsDto>
            GetFilterOptionsAsync(
                CancellationToken cancellationToken = default);
    }
}