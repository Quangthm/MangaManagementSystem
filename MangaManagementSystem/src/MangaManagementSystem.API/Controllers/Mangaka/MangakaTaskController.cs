using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.API.Controllers.Mangaka
{
    /// <summary>
    /// Thin HTTP boundary for Mangaka task-review workflows. Allows Mangaka to view
    /// submitted task output, approve/complete tasks, return for rework, and cancel.
    /// Uses the transitional X-Actor-User-Id header.
    /// </summary>
    [ApiController]
    [Route("api/mangaka/tasks")]
    public class MangakaTaskController : ControllerBase
    {
        private const string ActorUserIdHeader = "X-Actor-User-Id";

        private readonly IChapterPageTaskService _taskService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<MangakaTaskController> _logger;

        public MangakaTaskController(
            IChapterPageTaskService taskService,
            INotificationService notificationService,
            ILogger<MangakaTaskController> logger)
        {
            _taskService = taskService;
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Get all tasks created by this Mangaka for review.
        /// Route: GET /api/mangaka/tasks
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetTasksForReviewAsync()
        {
            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest("Could not identify the requesting user. Please sign in again.");
            }

            try
            {
                var tasks = await _taskService.GetTasksForReviewByCreatorAsync(actorUserId);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading tasks for review by user {ActorUserId}.", actorUserId);
                return Problem(
                    detail: "Could not load tasks right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get detail of a specific task created by this Mangaka.
        /// Route: GET /api/mangaka/tasks/{taskId}
        /// </summary>
        [HttpGet("{taskId:guid}")]
        public async Task<IActionResult> GetTaskDetailAsync(Guid taskId)
        {
            if (taskId == Guid.Empty)
            {
                return BadRequest("Invalid task ID.");
            }

            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest("Could not identify the requesting user. Please sign in again.");
            }

            try
            {
                // Use the full-context read so Mangaka can see submitted output
                var tasks = await _taskService.GetTasksForReviewByCreatorAsync(actorUserId);
                var task = tasks.FirstOrDefault(t => t.ChapterPageTaskId == taskId);
                if (task == null)
                {
                    return NotFound("Task not found or not created by current user.");
                }

                return Ok(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading task detail {TaskId} for Mangaka {ActorUserId}.", taskId, actorUserId);
                return Problem(
                    detail: "Could not load task detail right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Approve/complete a task. Task must be UNDER_REVIEW.
        /// Route: POST /api/mangaka/tasks/{taskId}/approve
        /// </summary>
        [HttpPost("{taskId:guid}/approve")]
        public async Task<IActionResult> ApproveTaskAsync(
            Guid taskId,
            [FromBody] MangakaTaskActionRequest? request)
        {
            if (taskId == Guid.Empty)
            {
                return BadRequest("Invalid task ID.");
            }

            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest("Could not identify the requesting user. Please sign in again.");
            }

            try
            {
                await _taskService.ApproveTaskAsync(actorUserId, taskId, request?.Reason);

                // Notify assistant
                await TryNotifyAssistantAsync(taskId, actorUserId, "TASK_COMPLETED",
                    "Task Approved", "Your submitted work has been approved.");

                return Ok(new { taskId, statusCode = "COMPLETED" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                _logger.LogWarning(ex, "SQL error approving task {TaskId}.", taskId);
                return BadRequest(MapSqlException(ex));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving task {TaskId}.", taskId);
                return Problem(
                    detail: "Could not approve task right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Return a task for rework. Task must be UNDER_REVIEW. Reason required.
        /// Route: POST /api/mangaka/tasks/{taskId}/return-for-rework
        /// </summary>
        [HttpPost("{taskId:guid}/return-for-rework")]
        public async Task<IActionResult> ReturnForReworkAsync(
            Guid taskId,
            [FromBody] MangakaTaskActionRequest? request)
        {
            if (taskId == Guid.Empty)
            {
                return BadRequest("Invalid task ID.");
            }

            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest("Could not identify the requesting user. Please sign in again.");
            }

            if (string.IsNullOrWhiteSpace(request?.Reason))
            {
                return BadRequest("A reason is required when returning a task for rework.");
            }

            try
            {
                await _taskService.ReturnTaskForReworkAsync(actorUserId, taskId, request.Reason.Trim());

                await TryNotifyAssistantAsync(taskId, actorUserId, "TASK_RETURNED_FOR_REWORK",
                    "Task Returned for Rework", $"Your submission was returned for rework: {request.Reason.Trim()}");

                return Ok(new { taskId, statusCode = "ASSIGNED" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                _logger.LogWarning(ex, "SQL error returning task {TaskId} for rework.", taskId);
                return BadRequest(MapSqlException(ex));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error returning task {TaskId} for rework.", taskId);
                return Problem(
                    detail: "Could not return task for rework right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Cancel a task. Task must be ASSIGNED or UNDER_REVIEW. Reason required.
        /// Route: POST /api/mangaka/tasks/{taskId}/cancel
        /// </summary>
        [HttpPost("{taskId:guid}/cancel")]
        public async Task<IActionResult> CancelTaskAsync(
            Guid taskId,
            [FromBody] MangakaTaskActionRequest? request)
        {
            if (taskId == Guid.Empty)
            {
                return BadRequest("Invalid task ID.");
            }

            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest("Could not identify the requesting user. Please sign in again.");
            }

            if (string.IsNullOrWhiteSpace(request?.Reason))
            {
                return BadRequest("A reason is required when cancelling a task.");
            }

            try
            {
                await _taskService.CancelTaskAsync(actorUserId, taskId, request.Reason.Trim());

                await TryNotifyAssistantAsync(taskId, actorUserId, "TASK_CANCELLED",
                    "Task Cancelled", $"Your task was cancelled: {request.Reason.Trim()}");

                return Ok(new { taskId, statusCode = "CANCELLED" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                _logger.LogWarning(ex, "SQL error cancelling task {TaskId}.", taskId);
                return BadRequest(MapSqlException(ex));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling task {TaskId}.", taskId);
                return Problem(
                    detail: "Could not cancel task right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get eligible assistants for task reassignment.
        /// Route: GET /api/mangaka/tasks/{taskId}/eligible-assistants
        /// </summary>
        [HttpGet("{taskId:guid}/eligible-assistants")]
        public async Task<IActionResult> GetEligibleAssistantsAsync(Guid taskId)
        {
            if (taskId == Guid.Empty)
            {
                return BadRequest("Invalid task ID.");
            }

            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest("Could not identify the requesting user. Please sign in again.");
            }

            try
            {
                var assistants = await _taskService.GetEligibleAssistantsForTaskAsync(actorUserId, taskId);
                return Ok(assistants);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading eligible assistants for task {TaskId}.", taskId);
                return Problem(
                    detail: "Could not load eligible assistants right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Reassign a task to a different assistant.
        /// Route: POST /api/mangaka/tasks/{taskId}/reassign
        /// </summary>
        [HttpPost("{taskId:guid}/reassign")]
        public async Task<IActionResult> ReassignTaskAsync(
            Guid taskId,
            [FromBody] ReassignChapterPageTaskRequest? request)
        {
            if (taskId == Guid.Empty)
            {
                return BadRequest("Invalid task ID.");
            }

            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest("Could not identify the requesting user. Please sign in again.");
            }

            if (request == null)
            {
                return BadRequest("Request body is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return BadRequest("A reason is required when reassigning a task.");
            }

            if (request.NewAssignedToUserId == Guid.Empty)
            {
                return BadRequest("New assigned user is required.");
            }

            try
            {
                var result = await _taskService.ReassignTaskAsync(actorUserId, taskId, request);

                // Notify new assistant about the assignment
                await TryNotifyAssistantByUserIdAsync(request.NewAssignedToUserId, actorUserId,
                    "TASK_ASSIGNED", "New Task Assignment",
                    "A task has been reassigned to you.");

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                _logger.LogWarning(ex, "SQL error reassigning task {TaskId}.", taskId);
                return BadRequest(MapSqlException(ex));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reassigning task {TaskId}.", taskId);
                return Problem(
                    detail: "Could not reassign task right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        // --- Helpers ---

        private async Task TryNotifyAssistantByUserIdAsync(Guid recipientUserId, Guid actorUserId, string typeCode, string title, string message)
        {
            try
            {
                if (recipientUserId != Guid.Empty)
                {
                    await _notificationService.CreateNotificationAsync(new CreateNotificationDto(
                        RecipientUserId: recipientUserId,
                        NotificationTypeCode: typeCode,
                        Title: title,
                        Message: message,
                        RelatedEntityType: "ChapterPageTask",
                        RelatedEntityId: Guid.Empty));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send notification to user {RecipientUserId}. Non-blocking.", recipientUserId);
            }
        }

        private async Task TryNotifyAssistantAsync(Guid taskId, Guid actorUserId, string typeCode, string title, string message)
        {
            try
            {
                // Load task to get the assigned user
                var tasks = await _taskService.GetTasksForReviewByCreatorAsync(actorUserId);
                var task = tasks.FirstOrDefault(t => t.ChapterPageTaskId == taskId);
                if (task != null && task.AssignedToUserId != Guid.Empty)
                {
                    await _notificationService.CreateNotificationAsync(new CreateNotificationDto(
                        RecipientUserId: task.AssignedToUserId,
                        NotificationTypeCode: typeCode,
                        Title: title,
                        Message: message,
                        RelatedEntityType: "ChapterPageTask",
                        RelatedEntityId: taskId));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send notification for task {TaskId}. Non-blocking.", taskId);
            }
        }

        private static string MapSqlException(Microsoft.Data.SqlClient.SqlException ex)
        {
            return ex.Number switch
            {
                58201 or 58301 or 58401 or 58501 => "Could not acquire task lock. Please try again.",
                58202 or 58302 or 58402 or 58502 => "Task does not exist.",
                58203 => "This task cannot be cancelled because it is not in the expected status.",
                58303 => "This task cannot be approved because it is not currently under review.",
                58403 => "Only tasks currently under review can be returned for rework.",
                58406 => "You must be an active contributor of this series to return a task for rework.",
                // Reassignment SP errors
                58503 => "Completed or cancelled tasks cannot be reassigned.",
                58504 => "New assigned user must be different from the current assignee.",
                58505 => "A reason is required when reassigning a task.",
                58508 => "New assigned user must be an active contributor of the same series.",
                _ => ex.Message
            };
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

    /// <summary>
    /// Request body for Mangaka task actions (approve/return/cancel).
    /// </summary>
    public class MangakaTaskActionRequest
    {
        public string? Reason { get; set; }
    }
}
