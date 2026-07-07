using MangaManagementSystem.Application.DTOs.Admin;
using MediatR;

namespace MangaManagementSystem.Application.Features.Admin.Audit.Queries
{
    public sealed record SearchAdminAuditEventsQuery(
        string? Search,
        string? ActionCode,
        string? EntityType,
        DateTime? FromUtc,
        DateTime? ToUtc,
        int PageNumber = 1,
        int PageSize = 20)
        : IRequest<AdminAuditPageDto>;

    public sealed record GetAdminAuditFilterOptionsQuery
        : IRequest<AdminAuditFilterOptionsDto>;
}