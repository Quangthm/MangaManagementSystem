using System.Data;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
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

        public async Task<AuditEvent> AppendAsync(
            Guid? actorUserId,
            string actionCode,
            string entityType,
            string? entityId = null,
            string? detailJson = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(actionCode))
            {
                throw new InvalidOperationException(
                    "Audit action code is required.");
            }

            if (string.IsNullOrWhiteSpace(entityType))
            {
                throw new InvalidOperationException(
                    "Audit entity type is required.");
            }

            var connection =
                _context.Database.GetDbConnection();

            await using var command =
                connection.CreateCommand();

            command.CommandText =
                "audit.usp_AuditEvent_Append";

            command.CommandType =
                CommandType.StoredProcedure;

            command.Parameters.Add(
                new SqlParameter(
                    "@actor_user_id",
                    SqlDbType.UniqueIdentifier)
                {
                    Value =
                        (object?)actorUserId
                        ?? DBNull.Value
                });

            command.Parameters.Add(
                new SqlParameter(
                    "@action_code",
                    SqlDbType.NVarChar,
                    64)
                {
                    Value =
                        actionCode.Trim()
                            .ToUpperInvariant()
                });

            command.Parameters.Add(
                new SqlParameter(
                    "@entity_type",
                    SqlDbType.NVarChar,
                    128)
                {
                    Value = entityType.Trim()
                });

            command.Parameters.Add(
                new SqlParameter(
                    "@entity_id",
                    SqlDbType.NVarChar,
                    100)
                {
                    Value =
                        string.IsNullOrWhiteSpace(
                            entityId)
                            ? DBNull.Value
                            : entityId.Trim()
                });

            command.Parameters.Add(
                new SqlParameter(
                    "@detail_json",
                    SqlDbType.NVarChar,
                    -1)
                {
                    Value =
                        string.IsNullOrWhiteSpace(
                            detailJson)
                            ? DBNull.Value
                            : detailJson
                });

            var auditEventIdParameter =
                new SqlParameter(
                    "@audit_event_id",
                    SqlDbType.BigInt)
                {
                    Direction =
                        ParameterDirection.Output
                };

            command.Parameters.Add(
                auditEventIdParameter);

            if (connection.State
                != ConnectionState.Open)
            {
                await connection.OpenAsync(
                    cancellationToken);
            }

            await command.ExecuteNonQueryAsync(
                cancellationToken);

            if (auditEventIdParameter.Value
                is DBNull)
            {
                throw new InvalidOperationException(
                    "The audit procedure did not return an audit event id.");
            }

            var auditEventId =
                Convert.ToInt64(
                    auditEventIdParameter.Value);

            return await _context.AuditEvents
                .AsNoTracking()
                .SingleAsync(
                    item =>
                        item.AuditEventId ==
                        auditEventId,
                    cancellationToken);
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
