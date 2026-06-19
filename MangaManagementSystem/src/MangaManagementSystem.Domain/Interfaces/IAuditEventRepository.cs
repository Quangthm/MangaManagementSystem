using MangaManagementSystem.Domain.Entities;

namespace MangaManagementSystem.Domain.Interfaces
{
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
    }
}
