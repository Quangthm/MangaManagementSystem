using System.Security.Claims;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.DTOs.Admin;
using MangaManagementSystem.Application.Features.Admin.Users.Commands;
using MangaManagementSystem.Application.Features.Admin.Users.Queries;
using MangaManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MangaManagementSystem.API.Controllers
{
    [ApiController]
    [Authorize(Roles = "Admin")]
    [Route("api/admin/users")]
    public sealed class AdminUsersController
        : ControllerBase
    {
        private const string ActiveStatusCode =
            "ACTIVE";

        private const string AdminRoleName =
            "Admin";

        private readonly ISender _sender;
        private readonly IUserService _userService;
        private readonly ILogger<AdminUsersController>
            _logger;

        public AdminUsersController(
            ISender sender,
            IUserService userService,
            ILogger<AdminUsersController> logger)
        {
            _sender = sender;
            _userService = userService;
            _logger = logger;}

        [HttpGet]
        public async Task<IActionResult> GetUsersAsync(
            [FromQuery] string? status,
            CancellationToken cancellationToken)
        {
            var authorization =
                await ResolveAdminActorAsync();

            if (authorization.Error is not null)
            {
                return authorization.Error;
            }

            try
            {
                var users =
                    await _sender.Send(
                        new GetAdminUsersQuery(status),
                        cancellationToken);

                return Ok(users);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new ApiErrorResponse(
                        AdminUserErrorCodes.InvalidStatus,
                        ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to load admin users.");

                return AdminRequestFailed();
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchUsersAsync(
            [FromQuery] string? search,
            [FromQuery] string? status,
            [FromQuery] string? role,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var authorization =
                await ResolveAdminActorAsync();

            if (authorization.Error is not null)
            {
                return authorization.Error;
            }

            try
            {
                var page =
                    await _sender.Send(
                        new SearchAdminUsersQuery(
                            search,
                            status,
                            role,
                            pageNumber,
                            pageSize),
                        cancellationToken);

                return Ok(page);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new ApiErrorResponse(
                        AdminUserErrorCodes.InvalidFilter,
                        ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to search admin users.");

                return AdminRequestFailed();
            }
        }

        [HttpGet("status-counts")]
        public async Task<IActionResult>
            GetStatusCountsAsync(
                CancellationToken cancellationToken)
        {
            var authorization =
                await ResolveAdminActorAsync();

            if (authorization.Error is not null)
            {
                return authorization.Error;
            }

            try
            {
                var counts =
                    await _sender.Send(
                        new GetAdminUserStatusCountsQuery(),
                        cancellationToken);

                return Ok(counts);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to load admin user status counts.");

                return AdminRequestFailed();
            }
        }

        [HttpGet("{userId:guid}")]
        public async Task<IActionResult> GetUserAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            var authorization =
                await ResolveAdminActorAsync();

            if (authorization.Error is not null)
            {
                return authorization.Error;
            }

            var user =
                await _sender.Send(
                    new GetAdminUserDetailQuery(userId),
                    cancellationToken);

            if (user is null)
            {
                return NotFound(
                    new ApiErrorResponse(
                        AdminUserErrorCodes.UserNotFound,
                        "The requested user was not found."));
            }

            return Ok(user);
        }

        [HttpGet("{userId:guid}/audit")]
        public async Task<IActionResult> GetUserAuditAsync(
            Guid userId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var authorization =
                await ResolveAdminActorAsync();

            if (authorization.Error is not null)
            {
                return authorization.Error;
            }

            try
            {
                var page =
                    await _sender.Send(
                        new GetAdminUserAuditQuery(
                            userId,
                            pageNumber,
                            pageSize),
                        cancellationToken);

                return Ok(page);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new ApiErrorResponse(
                        AdminUserErrorCodes.InvalidFilter,
                        ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to load audit events for user {UserId}.",
                    userId);

                return AdminRequestFailed();
            }
        }

        [HttpGet("{userId:guid}/portfolio")]
        public async Task<IActionResult>
            GetPortfolioAsync(
                Guid userId,
                CancellationToken cancellationToken)
        {
            var authorization =
                await ResolveAdminActorAsync();

            if (authorization.Error is not null)
            {
                return authorization.Error;
            }

            if (userId == Guid.Empty)
            {
                return BadRequest(
                    new ApiErrorResponse(
                        AuthErrorCodes.InvalidRequest,
                        "User id is required."));
            }

            try
            {
                var file =
                    await _sender.Send(
                        new GetAdminUserPortfolioQuery(
                            userId),
                        cancellationToken);

                if (file is null)
                {
                    return NotFound(
                        new ApiErrorResponse(
                            AdminUserErrorCodes.PortfolioNotFound,
                            "Portfolio file was not found."));
                }

                return Ok(file);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to load portfolio for user {UserId}.",
                    userId);

                return AdminRequestFailed();
            }
        }

        [HttpPost("{userId:guid}/approve")]
        public Task<IActionResult> ApproveAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            return ExecuteUserCommandAsync(
                async actorUserId =>
                    await _sender.Send(
                        new ApproveAdminUserCommand(
                            actorUserId,
                            userId),
                        cancellationToken));
        }

        [HttpPost("{userId:guid}/reject")]
        public Task<IActionResult> RejectAsync(
            Guid userId,
            [FromBody] AdminUserActionRequest? request,
            CancellationToken cancellationToken)
        {
            return ExecuteUserCommandAsync(
                async actorUserId =>
                    await _sender.Send(
                        new RejectAdminUserCommand(
                            actorUserId,
                            userId,
                            request?.Reason),
                        cancellationToken));
        }

        [HttpPost("{userId:guid}/disable")]
        public Task<IActionResult> DisableAsync(
            Guid userId,
            [FromBody] AdminUserActionRequest? request,
            CancellationToken cancellationToken)
        {
            return ExecuteUserCommandAsync(
                async actorUserId =>
                    await _sender.Send(
                        new DisableAdminUserCommand(
                            actorUserId,
                            userId,
                            request?.Reason),
                        cancellationToken));
        }

        [HttpPost("{userId:guid}/activate")]
        public Task<IActionResult> ActivateAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            return ExecuteUserCommandAsync(
                async actorUserId =>
                    await _sender.Send(
                        new ActivateAdminUserCommand(
                            actorUserId,
                            userId),
                        cancellationToken));
        }

        [HttpPost("{userId:guid}/password-reset")]
        public async Task<IActionResult>
            SendPasswordResetAsync(
                Guid userId,
                [FromBody] AdminPasswordResetRequest request,
                CancellationToken cancellationToken)
        {
            var authorization =
                await ResolveAdminActorAsync();

            if (authorization.Error is not null)
            {
                return authorization.Error;
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(
                    new ApiErrorResponse(
                        AuthErrorCodes.ValidationFailed,
                        "A valid reset page URL is required."));
            }

            try
            {
                await _sender.Send(
                    new SendAdminPasswordResetCommand(
                        authorization.Actor!.UserId,
                        userId,
                        request.ResetPageUrl),
                    cancellationToken);

                return Ok(
                    new ApiMessageResponse(
                        "Password reset link sent."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new ApiErrorResponse(
                        AdminUserErrorCodes.PasswordResetFailed,
                        ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send an Admin password reset link for user {UserId}.",
                    userId);

                return AdminRequestFailed();
            }
        }

        private async Task<IActionResult>
            ExecuteUserCommandAsync(
                Func<Guid, Task<UserDto>> action)
        {
            var authorization =
                await ResolveAdminActorAsync();

            if (authorization.Error is not null)
            {
                return authorization.Error;
            }

            try
            {
                var updated =
                    await action(
                        authorization.Actor!.UserId);

                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(
                    new ApiErrorResponse(
                        AdminUserErrorCodes.InvalidTransition,
                        ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Admin user workflow failed.");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new ApiErrorResponse(
                        AdminUserErrorCodes.RequestFailed,
                        "The user account action could not be completed."));
            }
        }

        private IActionResult AdminRequestFailed()
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ApiErrorResponse(
                    AdminUserErrorCodes.RequestFailed,
                    "The administrator request could not be completed."));
        }

        private async Task<(
            UserDto? Actor,
            IActionResult? Error)>
            ResolveAdminActorAsync()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return (
                    null,
                    Unauthorized(
                        new ApiErrorResponse(
                            AdminUserErrorCodes.AccessDenied,
                            "Authentication is required.")));
            }

            var actorUserIdValue =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("user_id")?.Value
                ?? User.FindFirst("UserId")?.Value;

            if (!Guid.TryParse(
                    actorUserIdValue,
                    out var actorUserId)
                || actorUserId == Guid.Empty)
            {
                return (
                    null,
                    Unauthorized(
                        new ApiErrorResponse(
                            AdminUserErrorCodes.AccessDenied,
                            "Authenticated administrator information is invalid.")));
            }

            var actor =
                await _userService.GetUserByIdAsync(
                    actorUserId);

            if (actor is null)
            {
                return (
                    null,
                    Unauthorized(
                        new ApiErrorResponse(
                            AdminUserErrorCodes.AccessDenied,
                            "Authenticated administrator was not found.")));
            }

            if (!string.Equals(
                    actor.StatusCode,
                    ActiveStatusCode,
                    StringComparison.OrdinalIgnoreCase)
                || !string.Equals(
                    actor.RoleName,
                    AdminRoleName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return (
                    null,
                    StatusCode(
                        StatusCodes.Status403Forbidden,
                        new ApiErrorResponse(
                            AdminUserErrorCodes.AccessDenied,
                            "The current account is not an active administrator.")));
            }

            return (
                actor,
                null);
        }

    }
}
