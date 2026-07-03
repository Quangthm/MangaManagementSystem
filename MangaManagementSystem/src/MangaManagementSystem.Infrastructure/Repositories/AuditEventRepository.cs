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
                .Include(item => item.ActorUser)
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
                .Include(item => item.ActorUser)
                .Where(
                    item =>
                        item.EntityType ==
                            entityType
                        && item.EntityId ==
                            entityId)
                .OrderByDescending(
                    item =>
                        item.OccurredAtUtc)
                .ThenByDescending(
                    item =>
                        item.AuditEventId)
                .ToListAsync(
                    cancellationToken);
        }

        public async Task<(
            IReadOnlyList<AuditEvent> Items,
            int TotalCount)> GetForUserAsync(
                Guid userId,
                int pageNumber,
                int pageSize,
                CancellationToken cancellationToken = default)
        {
            if (userId == Guid.Empty)
            {
                return (
                    Array.Empty<AuditEvent>(),
                    0);
            }

            var userIdD =
                userId.ToString("D");

            var userIdN =
                userId.ToString("N");

            var query =
                _context.AuditEvents
                    .AsNoTracking()
                    .Include(item => item.ActorUser)
                    .Where(
                        item =>
                            (item.EntityType == "Users"
                                || item.EntityType == "USER")
                            && (item.EntityId == userIdD
                                || item.EntityId == userIdN));

            var totalCount =
                await query.CountAsync(
                    cancellationToken);

            var items =
                await query
                    .OrderByDescending(
                        item =>
                            item.OccurredAtUtc)
                    .ThenByDescending(
                        item =>
                            item.AuditEventId)
                    .Skip(
                        (pageNumber - 1)
                        * pageSize)
                    .Take(
                        pageSize)
                    .ToListAsync(
                        cancellationToken);

            return (
                items,
                totalCount);
        }

        public async Task<(
            IReadOnlyList<AuditEvent> Items,
            int TotalCount)> SearchAsync(
                AuditEventSearchCriteria criteria,
                CancellationToken cancellationToken = default)
        {
            var query =
                _context.AuditEvents
                    .AsNoTracking()
                    .Include(item => item.ActorUser)
                    .AsQueryable();

            if (!string.IsNullOrWhiteSpace(
                    criteria.Search))
            {
                var search = criteria.Search;

                query =
                    query.Where(
                        item =>
                            item.ActionCode.Contains(search)
                            || item.EntityType.Contains(search)
                            || (item.EntityId != null
                                && item.EntityId.Contains(search))
                            || (item.DetailJson != null
                                && item.DetailJson.Contains(search))
                            || (item.ActorRoleName != null
                                && item.ActorRoleName.Contains(search))
                            || (item.ActorUser != null
                                && (item.ActorUser.Username.Contains(search)
                                    || item.ActorUser.Email.Contains(search)
                                    || item.ActorUser.DisplayName.Contains(search))));
            }

            if (!string.IsNullOrWhiteSpace(
                    criteria.ActionCode))
            {
                query =
                    query.Where(
                        item =>
                            item.ActionCode ==
                            criteria.ActionCode);
            }

            if (!string.IsNullOrWhiteSpace(
                    criteria.EntityType))
            {
                query =
                    query.Where(
                        item =>
                            item.EntityType ==
                            criteria.EntityType);
            }

            if (criteria.FromUtc.HasValue)
            {
                query =
                    query.Where(
                        item =>
                            item.OccurredAtUtc >=
                            criteria.FromUtc.Value);
            }

            if (criteria.ToUtc.HasValue)
            {
                query =
                    query.Where(
                        item =>
                            item.OccurredAtUtc <=
                            criteria.ToUtc.Value);
            }

            var totalCount =
                await query.CountAsync(
                    cancellationToken);

            var items =
                await query
                    .OrderByDescending(
                        item =>
                            item.OccurredAtUtc)
                    .ThenByDescending(
                        item =>
                            item.AuditEventId)
                    .Skip(
                        (criteria.PageNumber - 1)
                        * criteria.PageSize)
                    .Take(
                        criteria.PageSize)
                    .ToListAsync(
                        cancellationToken);

            return (
                items,
                totalCount);
        }

        public async Task<IReadOnlyList<string>>
            GetDistinctActionCodesAsync(
                CancellationToken cancellationToken = default)
        {
            return await _context.AuditEvents
                .AsNoTracking()
                .Select(item => item.ActionCode)
                .Distinct()
                .OrderBy(item => item)
                .ToListAsync(
                    cancellationToken);
        }

        public async Task<IReadOnlyList<string>>
    GetDistinctEntityTypesAsync(
        CancellationToken cancellationToken = default)
        {
            return await _context.AuditEvents
                .AsNoTracking()
                .Select(item => item.EntityType)
                .Distinct()
                .OrderBy(item => item)
                .ToListAsync(
                    cancellationToken);
        }

        public async Task AddAsync(
    AuditEvent auditEvent,
    CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(auditEvent);

            await _context.AuditEvents.AddAsync(
                auditEvent,
                cancellationToken);
        }
    }
}