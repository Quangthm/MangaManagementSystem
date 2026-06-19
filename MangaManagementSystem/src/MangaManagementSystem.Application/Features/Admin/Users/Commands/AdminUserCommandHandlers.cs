using System.Text.Json;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Interfaces;
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
            UserDto>,
          IRequestHandler<
            SendAdminPasswordResetCommand,
            bool>
    {
        private readonly IUserService _userService;
        private readonly IUserRepository _userRepository;
        private readonly IAuthService _authService;
        private readonly IAuditEventRepository
            _auditEventRepository;

        public AdminUserCommandHandlers(
            IUserService userService,
            IUserRepository userRepository,
            IAuthService authService,
            IAuditEventRepository auditEventRepository)
        {
            _userService = userService;
            _userRepository = userRepository;
            _authService = authService;
            _auditEventRepository = auditEventRepository;
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

        public async Task<bool> Handle(
            SendAdminPasswordResetCommand request,
            CancellationToken cancellationToken)
        {
            if (request.TargetUserId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "Target user id is required.");
            }

            var user =
                await _userRepository.GetByIdWithRoleAsync(
                    request.TargetUserId,
                    cancellationToken)
                ?? throw new InvalidOperationException(
                    "The target user was not found.");

            await _authService.RequestPasswordResetAsync(
                user.Email,
                request.ResetPageUrl,
                cancellationToken);

            await _auditEventRepository.AppendAsync(
                request.ActorUserId,
                "ADMIN_PASSWORD_RESET_LINK_SENT",
                "Users",
                request.TargetUserId.ToString("D"),
                JsonSerializer.Serialize(
                    new
                    {
                        target_user_id =
                            request.TargetUserId,
                        delivery = "email"
                    }),
                cancellationToken);

            return true;
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
