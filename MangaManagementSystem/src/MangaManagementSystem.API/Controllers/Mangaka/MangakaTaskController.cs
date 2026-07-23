using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.API.Security;
using MangaManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.API.Controllers.Mangaka
{
    /// <summary>
    /// Thin HTTP boundary for Mangaka task-review workflows. Allows Mangaka to view
    /// submitted task output, approve/complete tasks, return for rework, and cancel.
    /// Uses the authenticated JWT actor and preserves existing task authorization.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/mangaka/tasks")]
    public class MangakaTaskController : ControllerBase
    {
        private const string MangakaRoleName = "Mangaka";

        private readonly IChapterPageTaskService _taskService;
        private readonly IAuthenticatedActorResolver _actorResolver;
        private readonly IWorkspaceResourceAuthorizationService _workspaceAccess;
        private readonly ILogger<MangakaTaskController> _logger;

        public MangakaTaskController(
            IChapterPageTaskService taskService,
            IAuthenticatedActorResolver actorResolver,
            IWorkspaceResourceAuthorizationService workspaceAccess,
            ILogger<MangakaTaskController> logger)
        {
            _taskService = taskService;
            _actorResolver = actorResolver;
            _workspaceAccess = workspaceAccess;
            _logger = logger;
        }

        /// <summary>
        /// Get all tasks created by this Mangaka for review.
        /// Route: GET /api/mangaka/tasks
        /// </summary>
        [HttpGet]
        [Authorize(Roles = MangakaRoleName)]
        public async Task<IActionResult> GetTasksForReviewAsync()
        {
            var (actorUserId, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null)
                return actorFailure;

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
        [Authorize(Roles = MangakaRoleName)]
        public async Task<IActionResult> GetTaskDetailAsync(Guid taskId)
        {
            if (taskId == Guid.Empty)
            {
                return BadRequest("Invalid task ID.");
            }

            var (actorUserId, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null)
                return actorFailure;

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
        [Authorize(Roles = "Mangaka,Tantou Editor")]
        public async Task<IActionResult> GetTasksByPageAsync(Guid chapterPageId)
        {
            var (actorUserId, actorFailure) = await ResolveActorAsync("Mangaka", "Tantou Editor");
            if (actorFailure is not null)
                return actorFailure;

            if (chapterPageId == Guid.Empty)
            {
                return BadRequest("Invalid page ID.");
            }
            if (!await _workspaceAccess.CanAccessPagesAsync(actorUserId, new[] { chapterPageId }, HttpContext.RequestAborted))
                return Forbid();

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
        [Authorize(Roles = MangakaRoleName)]
        public async Task<IActionResult> CreateTaskAsync([FromBody] CreateMangakaTaskRequest? request)
        {
            if (request == null)
            {
                return BadRequest("Task details are required.");
            }

            var (actorUserId, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null)
                return actorFailure;

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
                    DueAtUtc: request.DueAtUtc,
                    CompensationAmount: request.CompensationAmount,
                    CompletedPageVersionId: null,
                    PageRegionIds: request.PageRegionIds ?? new List<Guid>());

                var created = await _taskService.CreateChapterPageTaskAsync(dto);



                return Ok(created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                if (ex.Number == 50002)
                {
                    _logger.LogWarning(ex, "Could not acquire the required audit lock while creating a task.");
                    return BadRequest("Could not record the task audit right now. Please try again.");
                }

                _logger.LogError(ex, "Unexpected SQL error creating task.");
                return Problem(
                    detail: "Could not create the task right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
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
        [Authorize(Roles = MangakaRoleName)]
        public async Task<IActionResult> ApproveTaskAsync(
            Guid taskId,
            [FromBody] MangakaTaskActionRequest? request)
        {
            if (taskId == Guid.Empty)
            {
                return BadRequest("Invalid task ID.");
            }

            var (actorUserId, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null)
                return actorFailure;

            try
            {
                await _taskService.ApproveTaskAsync(actorUserId, taskId, request?.Reason);



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
        [Authorize(Roles = MangakaRoleName)]
        public async Task<IActionResult> ReturnForReworkAsync(
            Guid taskId,
            [FromBody] MangakaTaskActionRequest? request)
        {
            if (taskId == Guid.Empty)
            {
                return BadRequest("Invalid task ID.");
            }

            var (actorUserId, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null)
                return actorFailure;

            if (string.IsNullOrWhiteSpace(request?.Reason))
            {
                return BadRequest("A reason is required when returning a task for rework.");
            }

            try
            {
                await _taskService.ReturnTaskForReworkAsync(actorUserId, taskId, request.Reason.Trim());



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
        [Authorize(Roles = MangakaRoleName)]
        public async Task<IActionResult> CancelTaskAsync(
            Guid taskId,
            [FromBody] MangakaTaskActionRequest? request)
        {
            if (taskId == Guid.Empty)
            {
                return BadRequest("Invalid task ID.");
            }

            var (actorUserId, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null)
                return actorFailure;

            if (string.IsNullOrWhiteSpace(request?.Reason))
            {
                return BadRequest("A reason is required when cancelling a task.");
            }

            try
            {
                await _taskService.CancelTaskAsync(actorUserId, taskId, request.Reason.Trim());



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
        [Authorize(Roles = MangakaRoleName)]
        public async Task<IActionResult> GetEligibleAssistantsAsync(Guid taskId)
        {
            if (taskId == Guid.Empty)
            {
                return BadRequest("Invalid task ID.");
            }

            var (actorUserId, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null)
                return actorFailure;

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
        [Authorize(Roles = MangakaRoleName)]
        public async Task<IActionResult> ReassignTaskAsync(
            Guid taskId,
            [FromBody] ReassignChapterPageTaskRequest? request)
        {
            if (taskId == Guid.Empty)
            {
                return BadRequest("Invalid task ID.");
            }

            var (actorUserId, actorFailure) = await ResolveActorAsync();
            if (actorFailure is not null)
                return actorFailure;

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

        private async Task<(Guid ActorUserId, IActionResult? Failure)> ResolveActorAsync(
            params string[] allowedRoles)
        {
            var result = await _actorResolver.ResolveAsync(
                User,
                allowedRoles.Length == 0 ? new[] { MangakaRoleName } : allowedRoles);
            if (result.Succeeded)
            {
                return (result.ActorUserId, null);
            }

            var response = new ApiErrorResponse(
                result.FailureKind == AuthenticatedActorFailureKind.UserNotFound
                    ? "Authenticated Mangaka account was not found."
                    : result.FailureKind == AuthenticatedActorFailureKind.InvalidIdentity
                        ? "Authenticated Mangaka information is invalid."
                        : "The current account is not an active Mangaka.");

            return result.FailureKind is AuthenticatedActorFailureKind.InvalidIdentity
                or AuthenticatedActorFailureKind.UserNotFound
                ? (Guid.Empty, Unauthorized(response))
                : (Guid.Empty, StatusCode(StatusCodes.Status403Forbidden, response));
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
