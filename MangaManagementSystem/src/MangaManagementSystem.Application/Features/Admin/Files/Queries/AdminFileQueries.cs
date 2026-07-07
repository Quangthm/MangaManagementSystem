using MangaManagementSystem.Application.DTOs.Admin;
using MediatR;

namespace MangaManagementSystem.Application.Features.Admin.Files.Queries
{
    public sealed record SearchAdminFilesQuery(
        Guid ActorUserId,
        string? Search,
        string? FilePurposeCode,
        string? DeletedState,
        DateTime? FromUtc,
        DateTime? ToUtc,
        int PageNumber = 1,
        int PageSize = 20)
        : IRequest<AdminFilePageDto>;

    public sealed record GetAdminFileDetailQuery(
        Guid ActorUserId,
        Guid FileResourceId)
        : IRequest<AdminFileDetailDto?>;

    public sealed record GetAdminFileContentSourceQuery(
        Guid ActorUserId,
        Guid FileResourceId)
        : IRequest<AdminFileContentSourceDto?>;
}
