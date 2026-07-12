using MangaManagementSystem.Application.Common.Constants;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.API.Controllers.Mangaka;

/// <summary>
/// Thin HTTP boundary for Mangaka task-review workflows. Allows Mangaka to view
/// submitted task output, approve/complete tasks, return for rework, and cancel.
/// Uses the transitional X-Actor-User-Id header.
/// </summary>
[ApiController]
[Authorize]
[Route("api/mangaka/tasks")]
public sealed class MangakaTaskController : BaseApiController
{

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
        var actorUserId = ResolveActorUserId();

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

        var actorUserId = ResolveActorUserId();

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
    /// All tasks anchored to a specific chapter page (across versions), for the workspace panel.
    /// Route: GET /api/mangaka/tasks/by-page/{chapterPageId}
    /// </summary>
    [HttpGet("by-page/{chapterPageId:guid}")]
    public async Task<IActionResult> GetTasksByPageAsync(Guid chapterPageId)
    {
        if (chapterPageId == Guid.Empty)
        {
            return BadRequest("Invalid page ID.");
        }

        try
        {
            var tasks = await _taskService.GetChapterPageTasksByChapterPageIdAsync(chapterPageId);
            return Ok(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading tasks for page {ChapterPageId}.", chapterPageId);
            return Problem(
                detail: "Could not load tasks right now. Please try again later.",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Create a single page task assigned to an assistant. Actor (creator) from header.
    /// Route: POST /api/mangaka/tasks
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateTaskAsync([FromBody] CreateMangakaTaskRequest? request)
    {
        if (request == null)
        {
            return BadRequest("Task details are required.");
        }

        var actorUserId = ResolveActorUserId();

        try
        {
            var dto = new CreateChapterPageTaskDto(
                ActorUserId: actorUserId,
                AssignedToUserId: request.AssignedToUserId,
                TypeCode: request.TypeCode,
                StatusCode: "ASSIGNED",           // create SP owns the authoritative default
                TaskTitle: request.TaskTitle,
                TaskDescription: request.TaskDescription,
                PriorityLevel: request.PriorityLevel,
                DueAtUtc: null,
                CompensationAmount: request.CompensationAmount,
                CompletedPageVersionId: null,
                PageRegionIds: request.PageRegionIds ?? new List<Guid>());

            var created = await _taskService.CreateChapterPageTaskAsync(dto);

            await TryNotifyAssistantAsync(created.ChapterPageTaskId, actorUserId, "NEW_TASK_ASSIGNED",
                "New Task Assigned", "You have been assigned a new production task.");

            return Ok(created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            _logger.LogWarning(ex, "SQL error creating task.");
            return BadRequest(MapSqlException(ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating task.");
            return Problem(
                detail: "Could not create the task right now. Please try again later.",
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

        var actorUserId = ResolveActorUserId();

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

        var actorUserId = ResolveActorUserId();

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

        var actorUserId = ResolveActorUserId();

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

        var actorUserId = ResolveActorUserId();

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

        var actorUserId = ResolveActorUserId();

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
            MangakaTaskErrors.LockAcquisitionFailed or MangakaTaskErrors.ApproveLockFailed or MangakaTaskErrors.ReturnLockFailed or MangakaTaskErrors.ReassignLockFailed => "Could not acquire task lock. Please try again.",
            MangakaTaskErrors.TaskNotFound or MangakaTaskErrors.ApproveTaskNotFound or MangakaTaskErrors.ReturnTaskNotFound or MangakaTaskErrors.ReassignTaskNotFound => "Task does not exist.",
            MangakaTaskErrors.CancelWrongStatus => "This task cannot be cancelled because it is not in the expected status.",
            MangakaTaskErrors.ApproveWrongStatus => "This task cannot be approved because it is not currently under review.",
            MangakaTaskErrors.ReturnWrongStatus => "Only tasks currently under review can be returned for rework.",
            MangakaTaskErrors.ReturnNotContributor => "You must be an active contributor of this series to return a task for rework.",
            MangakaTaskErrors.ReassignCompletedOrCancelled => "Completed or cancelled tasks cannot be reassigned.",
            MangakaTaskErrors.ReassignSameUser => "New assigned user must be different from the current assignee.",
            MangakaTaskErrors.ReassignReasonRequired => "A reason is required when reassigning a task.",
            MangakaTaskErrors.ReassignNotContributor => "New assigned user must be an active contributor of the same series.",
            _ => ex.Message
        };
    }

}

/// <summary>
/// Request body for Mangaka task actions (approve/return/cancel).
/// </summary>
public class MangakaTaskActionRequest
{
    public string? Reason { get; set; }
}

