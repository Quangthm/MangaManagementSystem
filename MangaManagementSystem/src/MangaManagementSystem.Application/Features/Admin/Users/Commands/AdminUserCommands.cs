using MangaManagementSystem.Application.DTOs.Auth;
using MediatR;

namespace MangaManagementSystem.Application.Features.Admin.Users.Commands
{
    public sealed record ApproveAdminUserCommand(
        Guid ActorUserId,
        Guid TargetUserId)
        : IRequest<UserDto>;

    public sealed record RejectAdminUserCommand(
        Guid ActorUserId,
        Guid TargetUserId,
        string? Reason = null)
        : IRequest<UserDto>;

    public sealed record DisableAdminUserCommand(
        Guid ActorUserId,
        Guid TargetUserId,
        string? Reason = null)
        : IRequest<UserDto>;

    public sealed record ActivateAdminUserCommand(
        Guid ActorUserId,
        Guid TargetUserId)
        : IRequest<UserDto>;

    public sealed record SendAdminPasswordResetCommand(
        Guid ActorUserId,
        Guid TargetUserId,
        string ResetPageUrl)
        : IRequest<bool>;
}
