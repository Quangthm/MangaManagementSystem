using System;
using System.Collections.Generic;

namespace MangaManagementSystem.Application.DTOs.Admin
{
    public sealed record AdminUserStatusCountsDto(
        int PendingApproval,
        int Active,
        int Disabled,
        int Rejected,
        int Total);

    public sealed record AdminUserListItemDto(
        Guid UserId,
        string Username,
        string Email,
        string? DisplayName,
        string? RoleName,
        string StatusCode,
        Guid? AvatarFileId,
        Guid? PortfolioFileId,
        DateTime CreatedAtUtc);

    public sealed record AdminUserDetailDto(
        Guid UserId,
        string Username,
        string Email,
        string? DisplayName,
        string? RoleName,
        string StatusCode,
        Guid? AvatarFileId,
        Guid? PortfolioFileId,
        DateTime CreatedAtUtc);

    public sealed record AdminUserPageDto(
        IReadOnlyList<AdminUserListItemDto> Items,
        int PageNumber,
        int PageSize,
        int TotalCount,
        int TotalPages);

    public sealed record AdminUserAuditEventDto(
        long AuditEventId,
        DateTime OccurredAtUtc,
        Guid? ActorUserId,
        string? ActorRoleName,
        string ActionCode,
        string EntityType,
        string? EntityId,
        string? DetailJson);

    public sealed record AdminUserAuditPageDto(
        IReadOnlyList<AdminUserAuditEventDto> Items,
        int PageNumber,
        int PageSize,
        int TotalCount,
        int TotalPages);
}
