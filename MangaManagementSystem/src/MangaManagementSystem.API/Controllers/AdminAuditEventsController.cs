using System.Security.Cryptography;
using System.Text;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.API.Options;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Features.Admin.Audit.Queries;
using MangaManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MangaManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/admin/audit-events")]
    public sealed class AdminAuditEventsController
        : ControllerBase
    {
        private const string ActiveStatusCode =
            "ACTIVE";

        private const string AdminRoleName =
            "Admin";

        private static class AdminAuditErrorCodes
        {
            internal const string InvalidFilter =
                "ADMIN_AUDIT_INVALID_FILTER";

            internal const string RequestFailed =
                "ADMIN_AUDIT_REQUEST_FAILED";

            internal const string AccessDenied =
                "ADMIN_AUDIT_ACCESS_DENIED";
        }

        private readonly ISender _sender;
        private readonly IUserService _userService;
        private readonly ILogger<AdminAuditEventsController>
            _logger;
        private readonly InternalApiOptions
            _internalApiOptions;

        public AdminAuditEventsController(
            ISender sender,
            IUserService userService,
            ILogger<AdminAuditEventsController> logger,
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
        public async Task<IActionResult> SearchAsync(
            [FromQuery] string? search,
            [FromQuery] string? actionCode,
            [FromQuery] string? entityType,
            [FromQuery] DateTime? fromUtc,
            [FromQuery] DateTime? toUtc,
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
                        new SearchAdminAuditEventsQuery(
                            search,
                            actionCode,
                            entityType,
                            fromUtc,
                            toUtc,
                            pageNumber,
                            pageSize),
                        cancellationToken);

                return Ok(page);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new ApiErrorResponse(
                        AdminAuditErrorCodes.InvalidFilter,
                        ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to search audit events.");

                return AuditRequestFailed();
            }
        }

        [HttpGet("filter-options")]
        public async Task<IActionResult>
            GetFilterOptionsAsync(
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
                var options =
                    await _sender.Send(
                        new GetAdminAuditFilterOptionsQuery(),
                        cancellationToken);

                return Ok(options);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to load audit filter options.");

                return AuditRequestFailed();
            }
        }

        private IActionResult AuditRequestFailed()
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ApiErrorResponse(
                    AdminAuditErrorCodes.RequestFailed,
                    "The audit request could not be completed."));
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
                    "Rejected unauthorized internal audit request.");

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
                            AdminAuditErrorCodes.AccessDenied,
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
                            AdminAuditErrorCodes.AccessDenied,
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
                            AdminAuditErrorCodes.AccessDenied,
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
                            AdminAuditErrorCodes.AccessDenied,
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