using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;

namespace MangaManagementSystem.Application.Services
{
    public sealed class AuditEventService
        : IAuditEventService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AuditEventService(
            IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<AuditEventDto>
            CreateAuditEventAsync(
                CreateAuditEventDto dto)
        {
            var entity =
                await _unitOfWork.AuditEvents
                    .AppendAsync(
                        dto.ActorUserId,
                        dto.ActionCode,
                        dto.EntityType,
                        dto.EntityId,
                        dto.DetailJson);

            return MapToDto(entity);
        }

        public async Task<AuditEventDto?>
            GetAuditEventByIdAsync(
                long id)
        {
            var entity =
                await _unitOfWork.AuditEvents
                    .GetByIdAsync(id);

            return entity is null
                ? null
                : MapToDto(entity);
        }

        public async Task<IEnumerable<AuditEventDto>>
            GetAuditEventsByEntityAsync(
                string entityType,
                string entityId)
        {
            var entities =
                await _unitOfWork.AuditEvents
                    .GetByEntityAsync(
                        entityType,
                        entityId);

            return entities.Select(
                MapToDto);
        }

        private static AuditEventDto MapToDto(
            AuditEvent entity)
        {
            return new AuditEventDto(
                entity.AuditEventId,
                entity.OccurredAtUtc,
                entity.ActorUserId,
                entity.ActorRoleName,
                entity.ActionCode,
                entity.EntityType,
                entity.EntityId
                    ?? string.Empty,
                entity.DetailJson);
        }
    }
}
