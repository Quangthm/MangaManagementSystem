using System.Security.Cryptography;
using System.Text;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.API.Options;
using MangaManagementSystem.Application.DTOs.Admin;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Features.Admin.Users.Commands;
using MangaManagementSystem.Application.Features.Admin.Users.Queries;
using MangaManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MangaManagementSystem.API.Controllers
{
    [ApiController]
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
        private readonly InternalApiOptions
            _internalApiOptions;

        public AdminUsersController(
            ISender sender,
            IUserService userService,
            ILogger<AdminUsersController> logger,
            IOptions<InternalApiOptions>
                internalApiOptions)
        {
            _sender = sender;
            _userService = userService;
            _logger = logger;
            _internalApiOptions =
                internalApiOptions.Value;
        }

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
                        new GetAdminUsersQuery(
                            status),
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
            return ExecuteCommandAsync(
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
            return ExecuteCommandAsync(
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
            return ExecuteCommandAsync(
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
            return ExecuteCommandAsync(
                async actorUserId =>
                    await _sender.Send(
                        new ActivateAdminUserCommand(
                            actorUserId,
                            userId),
                        cancellationToken));
        }

        private async Task<IActionResult>
            ExecuteCommandAsync(
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
            if (!Request.Headers.TryGetValue(
                    InternalApiOptions.HeaderName,
                    out var suppliedKey)
                || !KeysMatch(
                    suppliedKey.ToString(),
                    _internalApiOptions.Key))
            {
                _logger.LogWarning(
                    "Rejected unauthorized internal admin request.");

                return (
                    null,
                    Unauthorized(
                        new ApiErrorResponse(
                            AuthErrorCodes.UnauthorizedInternalRequest,
                            "Unauthorized internal request.")));
            }

            if (!Request.Headers.TryGetValue(
                    InternalApiOptions.ActorUserIdHeaderName,
                    out var actorUserIdHeader)
                || !Guid.TryParse(
                    actorUserIdHeader.ToString(),
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

            if (!Request.Headers.TryGetValue(
                    InternalApiOptions.ActorRoleHeaderName,
                    out var actorRoleHeader)
                || !string.Equals(
                    actorRoleHeader.ToString(),
                    AdminRoleName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return (
                    null,
                    StatusCode(
                        StatusCodes.Status403Forbidden,
                        new ApiErrorResponse(
                            AdminUserErrorCodes.AccessDenied,
                            "Administrator access is required.")));
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

        private static bool KeysMatch(
            string suppliedKey,
            string expectedKey)
        {
            if (string.IsNullOrWhiteSpace(
                    suppliedKey)
                || string.IsNullOrWhiteSpace(
                    expectedKey))
            {
                return false;
            }

            var suppliedBytes =
                Encoding.UTF8.GetBytes(
                    suppliedKey);

            var expectedBytes =
                Encoding.UTF8.GetBytes(
                    expectedKey);

            return suppliedBytes.Length ==
                    expectedBytes.Length
                && CryptographicOperations
                    .FixedTimeEquals(
                        suppliedBytes,
                        expectedBytes);
        }
    }
}
