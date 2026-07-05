using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/notifications")]
    public class NotificationController : ControllerBase
    {
        private const string ActorUserIdHeader = "X-Actor-User-Id";

        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(
            INotificationService notificationService,
            ILogger<NotificationController> logger)
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet]
        public async Task<IActionResult> GetNotificationsAsync(CancellationToken cancellationToken)
        {
            if (!TryResolveActorUserId(out var actorUserId))
            {
                return BadRequest(new ApiErrorResponse(
                    "Could not identify the requesting user. Please sign in again."));
            }

            try
            {
                var notifications = await _notificationService.GetNotificationsByRecipientUserIdAsync(actorUserId);
                return Ok(notifications.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading notifications for user {ActorUserId}.", actorUserId);
                return Problem(
                    detail: "Could not load notifications right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("{notificationId:guid}/mark-read")]
        public async Task<IActionResult> MarkAsReadAsync(Guid notificationId)
        {
            if (notificationId == Guid.Empty)
            {
                return BadRequest("Invalid notification ID.");
            }

            try
            {
                var result = await _notificationService.MarkNotificationAsReadAsync(notificationId);
                if (result == null)
                {
                    return NotFound("Notification not found.");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {NotificationId} as read.", notificationId);
                return Problem(
                    detail: "Could not update notification right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        private bool TryResolveActorUserId(out Guid actorUserId)
        {
            actorUserId = Guid.Empty;

            if (Request.Headers.TryGetValue(ActorUserIdHeader, out var headerValues))
            {
                string? raw = headerValues.ToString();
                if (Guid.TryParse(raw, out actorUserId) && actorUserId != Guid.Empty)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
