using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    public sealed class AuditEventRepository
        : IAuditEventRepository
    {
        private readonly ApplicationDbContext _context;

        public AuditEventRepository(
            ApplicationDbContext context)
        {
            _context = context;
        }


        public Task<AuditEvent?> GetByIdAsync(
            long auditEventId,
            CancellationToken cancellationToken = default)
        {
            return _context.AuditEvents
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.AuditEventId ==
                        auditEventId,
                    cancellationToken);
        }

        public async Task<IReadOnlyList<AuditEvent>>
            GetByEntityAsync(
                string entityType,
                string entityId,
                CancellationToken cancellationToken = default)
        {
            return await _context.AuditEvents
                .AsNoTracking()
                .Where(
                    item =>
                        item.EntityType ==
                            entityType
                        && item.EntityId ==
                            entityId)
                .OrderByDescending(
                    item =>
                        item.OccurredAtUtc)
                .ToListAsync(
                    cancellationToken);
        }
    }
}
