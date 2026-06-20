using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.API.Controllers.Assistant
{
    /// <summary>
    /// Thin HTTP boundary for Assistant task workflows. Uses the transitional
    /// X-Actor-User-Id header — the Web host owns the Blazor cookie/session and
    /// forwards the logged-in user's id here.
    /// </summary>
    [ApiController]
    [Route("api/assistant/tasks")]
    public class AssistantTaskController : ControllerBase
    {
        private const string ActorUserIdHeader = "X-Actor-User-Id";

        private readonly IAssistantTaskSubmissionService _submissionService;
        private readonly IFileStorageService _fileStorageService;
        private readonly CloudinaryFileStorageFormAdapter _formAdapter;
        private readonly IChapterPageTaskService _chapterPageTaskService;
        private readonly IChapterPageAnnotationService _annotationService;
        private readonly ILogger<AssistantTaskController> _logger;

        public AssistantTaskController(
            IAssistantTaskSubmissionService submissionService,
            IFileStorageService fileStorageService,
            IChapterPageTaskService chapterPageTaskService,
            IChapterPageAnnotationService annotationService,
            ILogger<AssistantTaskController> logger)
        {
            _submissionService = submissionService ?? throw new ArgumentNullException(nameof(submissionService));
            _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
            _formAdapter = new CloudinaryFileStorageFormAdapter(fileStorageService);
            _chapterPageTaskService = chapterPageTaskService ?? throw new ArgumentNullException(nameof(chapterPageTaskService));
            _annotationService = annotationService ?? throw new ArgumentNullException(nameof(annotationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get all tasks assigned to the current Assistant user.
        /// Route: GET /api/assistant/tasks
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAssignedTasksAsync()
        {
            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest("Could not identify the requesting user. Please sign in again.");
            }

            try
            {
                var tasks = await _chapterPageTaskService.GetAssignedTasksForAssistantAsync(actorUserId);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading assigned tasks for user {ActorUserId}.", actorUserId);
                return Problem(
                    detail: "Could not load assigned tasks right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get detail of a specific task assigned to the current Assistant user.
        /// Route: GET /api/assistant/tasks/{taskId}
        /// </summary>
        [HttpGet("{taskId:guid}")]
        public async Task<IActionResult> GetAssignedTaskDetailAsync(Guid taskId)
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
                var task = await _chapterPageTaskService.GetAssignedTaskDetailForAssistantAsync(actorUserId, taskId);
                if (task == null)
                {
                    return NotFound("Task not found or not assigned to current user.");
                }

                return Ok(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading task detail {TaskId} for user {ActorUserId}.", taskId, actorUserId);
                return Problem(
                    detail: "Could not load task detail right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Submit assistant task work with file upload.
        /// Moves task from ASSIGNED → UNDER_REVIEW.
        /// Route: POST /api/assistant/tasks/{taskId}/submit-work
        /// </summary>
        [HttpPost("{taskId:guid}/submit-work")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SubmitWorkAsync(
            [FromRoute] Guid taskId,
            IFormFile file,
            [FromForm] string? versionNote = null)
        {
            if (taskId == Guid.Empty)
            {
                return BadRequest("Invalid task ID.");
            }

            if (!TryResolveActorUserId(out Guid actorUserId))
            {
                return BadRequest("Could not identify the requesting user. Please sign in again.");
            }

            if (file == null || file.Length <= 0)
            {
                return BadRequest("A file is required for submission.");
            }

            const long MaxFileSizeBytes = 10 * 1024 * 1024;
            if (file.Length > MaxFileSizeBytes)
            {
                return BadRequest($"File exceeds maximum size of {MaxFileSizeBytes / 1024 / 1024} MB.");
            }

            var allowedTypes = new[] { "image/png", "image/jpeg", "image/jpg", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType?.ToLowerInvariant()))
            {
                return BadRequest("Unsupported file type. Allowed: PNG, JPEG, WebP.");
            }

            FileUploadResultDto uploadResult;
            try
            {
                uploadResult = await _formAdapter.UploadFormFileAsync(file, "CHAPTER_PAGE_VERSION", null);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest($"File upload failed: {ex.Message}");
            }

            var request = new AssistantTaskSubmitRequestDto(
                ActorUserId: actorUserId,
                ChapterPageTaskId: taskId,
                StorageProviderCode: "CLOUDINARY",
                PublicId: uploadResult.PublicId,
                SecureUrl: uploadResult.SecureUrl,
                OriginalFileName: uploadResult.OriginalFileName,
                ContentType: uploadResult.ContentType,
                FileSizeBytes: uploadResult.FileSizeBytes,
                Sha256Hash: uploadResult.Sha256Hash ?? string.Empty,
                VersionNote: versionNote
            );

            try
            {
                var result = await _submissionService.SubmitTaskWorkAsync(request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest($"Submission failed: {ex.Message}");
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                _logger.LogError(ex,
                    "SQL error submitting work for task {TaskId} by actor {ActorUserId}. SQL error number {SqlErrorNumber}: {SqlErrorMessage}",
                    taskId, actorUserId, ex.Number, ex.Message);

                // Map known SP THROW codes to clear responses.
                // usp_ChapterPageTask_SubmitForReview uses 58101-58106.
                return ex.Number switch
                {
                    // usp_ChapterPageTask_SubmitForReview error codes
                    58101 => StatusCode(StatusCodes.Status409Conflict, "The task is being processed. Please try again."),
                    58102 => NotFound("Task does not exist."),
                    58103 => BadRequest("This task must be in ASSIGNED status to submit work."),
                    58104 => StatusCode(StatusCodes.Status403Forbidden, "This task is not assigned to you."),
                    58105 => BadRequest("Submitted page version does not exist."),
                    58106 => BadRequest("Submitted page version must belong to the same chapter page as the task."),
                    // usp_FileResource_Create / unique constraint violations
                    2627 or 2601 => StatusCode(StatusCodes.Status409Conflict, "This file appears to have already been submitted."),
                    _ => Problem(
                        detail: "Could not submit work right now. Please try again later.",
                        statusCode: StatusCodes.Status500InternalServerError)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error submitting work for task {TaskId} by actor {ActorUserId}. {ExceptionMessage} | Inner: {InnerMessage}",
                    taskId, actorUserId, ex.Message, ex.InnerException?.Message);
                return Problem(
                    detail: "Could not submit work right now. Please try again later.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get annotations linked to the assigned page regions for a task.
        /// Route: GET /api/assistant/tasks/{taskId}/annotations
        /// </summary>
        [HttpGet("{taskId:guid}/annotations")]
        public async Task<IActionResult> GetTaskAnnotationsAsync(Guid taskId)
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
                // Load task to get assigned page region IDs and validate ownership
                var task = await _chapterPageTaskService.GetAssignedTaskDetailForAssistantAsync(actorUserId, taskId);
                if (task == null)
                {
                    return NotFound("Task not found or not assigned to current user.");
                }

                // Extract page region IDs from the task
                var regionIds = task.PageRegions.Select(r => r.PageRegionId).ToList();
                if (regionIds.Count == 0)
                {
                    return Ok(Array.Empty<ChapterPageAnnotationDto>());
                }

                // Query annotations for these regions
                var annotations = await _annotationService.GetAnnotationsByPageRegionIdsAsync(regionIds);
                return Ok(annotations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading annotations for task {TaskId}.", taskId);
                return Problem(
                    detail: "Could not load annotations right now. Please try again later.",
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
