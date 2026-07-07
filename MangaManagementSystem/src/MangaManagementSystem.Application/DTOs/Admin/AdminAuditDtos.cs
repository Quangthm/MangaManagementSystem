using System;
using System.Collections.Generic;

namespace MangaManagementSystem.Application.DTOs.Admin
{
    public sealed record AdminAuditEventDto(
        long AuditEventId,
        DateTime OccurredAtUtc,
        Guid? ActorUserId,
        string? ActorUsername,
        string? ActorDisplayName,
        string? ActorRoleName,
        string ActionCode,
        string EntityType,
        string? EntityId,
        string? DetailJson);

    public sealed record AdminAuditPageDto(
        IReadOnlyList<AdminAuditEventDto> Items,
        int PageNumber,
        int PageSize,
        int TotalCount,
        int TotalPages);

    public sealed record AdminAuditFilterOptionsDto(
        IReadOnlyList<string> ActionCodes,
        IReadOnlyList<string> EntityTypes);
}