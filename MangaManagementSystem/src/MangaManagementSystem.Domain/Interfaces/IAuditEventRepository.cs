using MangaManagementSystem.Domain.Entities;

namespace MangaManagementSystem.Domain.Interfaces
{
    public interface IAuditEventRepository
    {
        Task<AuditEvent> AppendAsync(
            Guid? actorUserId,
            string actionCode,
            string entityType,
            string? entityId = null,
            string? detailJson = null,
            CancellationToken cancellationToken = default);

        Task<AuditEvent?> GetByIdAsync(
            long auditEventId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AuditEvent>> GetByEntityAsync(
            string entityType,
            string entityId,
            CancellationToken cancellationToken = default);
    }
}
