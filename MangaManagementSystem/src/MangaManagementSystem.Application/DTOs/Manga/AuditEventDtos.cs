using System;
using System.ComponentModel.DataAnnotations;

namespace MangaManagementSystem.Application.DTOs.Manga
{
    public record AuditEventDto(
        long AuditEventId,
        DateTime OccurredAtUtc,
        Guid? ActorUserId,
        string? ActorRoleName,
        string ActionCode,
        string EntityType,
        string EntityId,
        string? DetailJson
    );

    public record CreateAuditEventDto(
        Guid? ActorUserId,
        [MaxLength(100)] string? ActorRoleName,
        [Required][MaxLength(100)] string ActionCode,
        [Required][MaxLength(100)] string EntityType,
        [Required][MaxLength(100)] string EntityId,
        string? DetailJson
    );
}
