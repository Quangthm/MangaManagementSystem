using MangaManagementSystem.Domain.Entities;

namespace MangaManagementSystem.Domain.Interfaces
{
    public sealed record AuditEventSearchCriteria(
        string? Search,
        string? ActionCode,
        string? EntityType,
        DateTime? FromUtc,
        DateTime? ToUtc,
        int PageNumber,
        int PageSize);

    public interface IAuditEventRepository
    {
        Task<AuditEvent?> GetByIdAsync(
            long auditEventId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AuditEvent>> GetByEntityAsync(
            string entityType,
            string entityId,
            CancellationToken cancellationToken = default);

        Task<(IReadOnlyList<AuditEvent> Items, int TotalCount)>
            GetForUserAsync(
                Guid userId,
                int pageNumber,
                int pageSize,
                CancellationToken cancellationToken = default);

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