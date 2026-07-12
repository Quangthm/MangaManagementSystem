using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;

namespace MangaManagementSystem.Application.Services;

public class AuditEventService : IAuditEventService
{
<<<<<<< HEAD
    private readonly IUnitOfWork _unitOfWork;

    public AuditEventService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
=======
    public sealed class AuditEventService
        : IAuditEventService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AuditEventService(
            IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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
>>>>>>> main
    }

    public async Task<AuditEventDto> CreateAuditEventAsync(CreateAuditEventDto dto)
    {
        var entity = new AuditEvent
        {
            OccurredAtUtc = DateTime.UtcNow,
            ActorUserId = dto.ActorUserId,
            ActorRoleName = dto.ActorRoleName,
            ActionCode = dto.ActionCode,
            EntityType = dto.EntityType,
            EntityId = dto.EntityId,
            DetailJson = dto.DetailJson
        };
        await _unitOfWork.AuditEvents.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<AuditEventDto?> GetAuditEventByIdAsync(long id)
    {
        var entity = await _unitOfWork.AuditEvents.GetByIdAsync(id).ConfigureAwait(false);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<IEnumerable<AuditEventDto>> GetAuditEventsByEntityAsync(string entityType, string entityId)
    {
        var all = await _unitOfWork.AuditEvents.GetAllAsync();
        return all
            .Where(e => e.EntityType == entityType && e.EntityId == entityId)
            .OrderByDescending(e => e.OccurredAtUtc)
            .Select(MapToDto);
    }

    private static AuditEventDto MapToDto(AuditEvent e) => new(
        e.AuditEventId,
        e.OccurredAtUtc,
        e.ActorUserId,
        e.ActorRoleName,
        e.ActionCode,
        e.EntityType,
        e.EntityId ?? string.Empty,
        e.DetailJson
    );
}
