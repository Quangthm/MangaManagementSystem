using MangaManagementSystem.Domain.Entities;

namespace MangaManagementSystem.Application.Interfaces
{
    public sealed record AuditEventSearchCriteria(
        string? Search,
        string? ActionCode,
        string? EntityType,
        DateTime? FromUtc,
        DateTime? ToUtc,
        int PageNumber,
        int PageSize);

    public interface IAdminAuditQueryRepository
    {
        Task<(IReadOnlyList<AuditEvent> Items, int TotalCount)>
            SearchAsync(
                AuditEventSearchCriteria criteria,
                CancellationToken cancellationToken = default);

        Task<IReadOnlyList<string>>
            GetDistinctActionCodesAsync(
                CancellationToken cancellationToken = default);

        Task<IReadOnlyList<string>>
            GetDistinctEntityTypesAsync(
                CancellationToken cancellationToken = default);
    }
}
