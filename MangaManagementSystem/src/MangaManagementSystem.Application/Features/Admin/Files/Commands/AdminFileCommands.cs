using MangaManagementSystem.Application.DTOs.Admin;
using MediatR;

namespace MangaManagementSystem.Application.Features.Admin.Files.Commands
{
    public sealed record SoftDeleteAdminFileCommand(
        Guid ActorUserId,
        Guid FileResourceId,
        string? DeleteReason)
        : IRequest<AdminFileDetailDto>;
}
