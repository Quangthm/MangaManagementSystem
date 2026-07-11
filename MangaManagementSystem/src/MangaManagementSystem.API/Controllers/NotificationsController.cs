using System.Security.Claims;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.Application.Features.Notifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MangaManagementSystem.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/notifications")]
    public sealed class NotificationsController
        : ControllerBase
    {
        private static class NotificationErrorCodes
        {
            internal const string AccessDenied =
                "NOTIFICATION_ACCESS_DENIED";

            internal const string InvalidRequest =
                "NOTIFICATION_INVALID_REQUEST";

            internal const string NotFound =
                "NOTIFICATION_NOT_FOUND";

            internal const string RequestFailed =
                "NOTIFICATION_REQUEST_FAILED";
        }

        private readonly ISender _sender;
        private readonly ILogger<NotificationsController>
            _logger;

        public NotificationsController(
            ISender sender,
            ILogger<NotificationsController> logger)
        {
            _sender =
                sender
                ?? throw new ArgumentNullException(
                    nameof(sender));

            _logger =
                logger
                ?? throw new ArgumentNullException(
                    nameof(logger));
        }

        [HttpGet]
        public async Task<IActionResult>
            GetCurrentUserNotificationsAsync(
                [FromQuery] int skip = 0,
                [FromQuery] int take = 20,
                CancellationToken cancellationToken = default)
        {
            if (!TryResolveCurrentUserId(
                    out var currentUserId))
            {
                return AuthenticationRequired();
            }

            try
            {
                var notifications =
                    await _sender.Send(
                        new GetCurrentUserNotificationsQuery(
                            currentUserId,
                            skip,
                            take),
                        cancellationToken);

                return Ok(notifications);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new ApiErrorResponse(
                        NotificationErrorCodes.InvalidRequest,
                        ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to load notifications for user {UserId}.",
                    currentUserId);

                return NotificationRequestFailed();
            }
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult>
            GetUnreadCountAsync(
                CancellationToken cancellationToken = default)
        {
            if (!TryResolveCurrentUserId(
                    out var currentUserId))
            {
                return AuthenticationRequired();
            }

            try
            {
                var result =
                    await _sender.Send(
                        new GetUnreadNotificationCountQuery(
                            currentUserId),
                        cancellationToken);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new ApiErrorResponse(
                        NotificationErrorCodes.InvalidRequest,
                        ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to load unread notification count for user {UserId}.",
                    currentUserId);

                return NotificationRequestFailed();
            }
        }

        [HttpPost("{notificationId:guid}/read")]
        public async Task<IActionResult>
            MarkAsReadAsync(
                Guid notificationId,
                CancellationToken cancellationToken = default)
        {
            if (!TryResolveCurrentUserId(
                    out var currentUserId))
            {
                return AuthenticationRequired();
            }

            if (notificationId == Guid.Empty)
            {
                return BadRequest(
                    new ApiErrorResponse(
                        NotificationErrorCodes.InvalidRequest,
                        "Notification id is required."));
            }

            try
            {
                var succeeded =
                    await _sender.Send(
                        new MarkNotificationAsReadCommand(
                            currentUserId,
                            notificationId),
                        cancellationToken);

                if (!succeeded)
                {
                    return NotFound(
                        new ApiErrorResponse(
                            NotificationErrorCodes.NotFound,
                            "The notification was not found."));
                }

                return Ok(
                    new ApiMessageResponse(
                        "Notification marked as read."));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new ApiErrorResponse(
                        NotificationErrorCodes.InvalidRequest,
                        ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to mark notification {NotificationId} as read for user {UserId}.",
                    notificationId,
                    currentUserId);

                return NotificationRequestFailed();
            }
        }

        [HttpPost("read-all")]
        public async Task<IActionResult>
            MarkAllAsReadAsync(
                CancellationToken cancellationToken = default)
        {
            if (!TryResolveCurrentUserId(
                    out var currentUserId))
            {
                return AuthenticationRequired();
            }

            try
            {
                var result =
                    await _sender.Send(
                        new MarkAllNotificationsAsReadCommand(
                            currentUserId),
                        cancellationToken);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new ApiErrorResponse(
                        NotificationErrorCodes.InvalidRequest,
                        ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to mark all notifications as read for user {UserId}.",
                    currentUserId);

                return NotificationRequestFailed();
            }
        }

        private bool TryResolveCurrentUserId(
            out Guid currentUserId)
        {
            currentUserId =
                Guid.Empty;

            if (User.Identity?.IsAuthenticated
                != true)
            {
                return false;
            }

            var value =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub")
                ?? User.FindFirstValue("user_id")
                ?? User.FindFirstValue("UserId");

            return Guid.TryParse(
                    value,
                    out currentUserId)
                && currentUserId != Guid.Empty;
        }

        private IActionResult AuthenticationRequired()
        {
            return Unauthorized(
                new ApiErrorResponse(
                    NotificationErrorCodes.AccessDenied,
                    "Authentication is required."));
        }

        private IActionResult
            NotificationRequestFailed()
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ApiErrorResponse(
                    NotificationErrorCodes.RequestFailed,
                    "The notification request could not be completed."));
        }
    }
}
