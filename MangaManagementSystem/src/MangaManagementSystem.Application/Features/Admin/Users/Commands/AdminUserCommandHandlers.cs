using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Admin.Users.Commands
{
    public sealed class AdminUserCommandHandlers
        : IRequestHandler<
            ApproveAdminUserCommand,
            UserDto>,
          IRequestHandler<
            RejectAdminUserCommand,
            UserDto>,
          IRequestHandler<
            DisableAdminUserCommand,
            UserDto>,
          IRequestHandler<
            ActivateAdminUserCommand,
            UserDto>
    {
        private readonly IUserService _userService;

        public AdminUserCommandHandlers(
            IUserService userService)
        {
            _userService = userService;
        }

        public Task<UserDto> Handle(
            ApproveAdminUserCommand request,
            CancellationToken cancellationToken)
        {
            return _userService.ApproveUserAsync(
                request.ActorUserId,
                request.TargetUserId);
        }

        public async Task<UserDto> Handle(
            RejectAdminUserCommand request,
            CancellationToken cancellationToken)
        {
            EnsureDifferentUsers(
                request.ActorUserId,
                request.TargetUserId);

            await _userService.RejectUserAsync(
                request.ActorUserId,
                request.TargetUserId,
                request.Reason);

            return await GetRequiredUserAsync(
                request.TargetUserId);
        }

        public Task<UserDto> Handle(
            DisableAdminUserCommand request,
            CancellationToken cancellationToken)
        {
            EnsureDifferentUsers(
                request.ActorUserId,
                request.TargetUserId);

            return _userService.DisableUserAsync(
                request.ActorUserId,
                request.TargetUserId,
                request.Reason);
        }

        public Task<UserDto> Handle(
            ActivateAdminUserCommand request,
            CancellationToken cancellationToken)
        {
            return _userService.ActivateUserAsync(
                request.ActorUserId,
                request.TargetUserId);
        }

        private async Task<UserDto> GetRequiredUserAsync(
            Guid userId)
        {
            return await _userService
                .GetUserByIdAsync(userId)
                ?? throw new InvalidOperationException(
                    $"User {userId} was not found.");
        }

        private static void EnsureDifferentUsers(
            Guid actorUserId,
            Guid targetUserId)
        {
            if (actorUserId == targetUserId)
            {
                throw new InvalidOperationException(
                    "An administrator cannot disable or reject their own account.");
            }
        }
    }
}
