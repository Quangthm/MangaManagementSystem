using MangaManagementSystem.Application.DTOs.Admin;
using MediatR;

namespace MangaManagementSystem.Application.Features.Admin.Files.Commands
{
    public sealed record SoftDeleteAdminFileCommand(
        Guid ActorUserId,
        Guid FileResourceId,
        string? DeleteReason)
        : IRequest<AdminFileDetailDto>;

    public sealed record CleanupAdminFileStorageCommand(
        Guid ActorUserId,
        Guid FileResourceId,
        string? Reason)
        : IRequest<AdminFileStorageCleanupResultDto>;

    public sealed record CleanupDeletedAdminFilesCommand(
        Guid ActorUserId)
        : IRequest<AdminFileStorageCleanupBatchResultDto>;
}
