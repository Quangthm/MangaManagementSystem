using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Services
{
    public class AuditEventService : IAuditEventService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AuditEventService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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
}
