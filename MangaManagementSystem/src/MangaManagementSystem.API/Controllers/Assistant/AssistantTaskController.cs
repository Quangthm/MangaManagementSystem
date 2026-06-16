using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MangaManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/assistant/tasks")]
    [Authorize(Roles = "Assistant")]
    public class AssistantTaskController : ControllerBase
    {
        private readonly IAssistantTaskSubmissionService _submissionService;
        private readonly IFileStorageService _fileStorageService;
        private readonly CloudinaryFileStorageFormAdapter _formAdapter;
        private readonly IChapterPageTaskService _chapterPageTaskService;

        public AssistantTaskController(
            IAssistantTaskSubmissionService submissionService,
            IFileStorageService fileStorageService,
            IChapterPageTaskService chapterPageTaskService)
        {
            _submissionService = submissionService ?? throw new ArgumentNullException(nameof(submissionService));
            _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
            _formAdapter = new CloudinaryFileStorageFormAdapter(fileStorageService);
            _chapterPageTaskService = chapterPageTaskService ?? throw new ArgumentNullException(nameof(chapterPageTaskService));
        }

        /// <summary>
        /// Get all tasks assigned to the current Assistant user.
        /// Assistant can only see tasks assigned to themselves.
        /// </summary>
        /// <returns>List of assigned tasks with full context.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ChapterPageTaskDto>>> GetAssignedTasksAsync()
        {
            // Get actor user ID from authenticated claims
            var actorUserIdClaim = User.FindFirst("sub") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (actorUserIdClaim == null || !Guid.TryParse(actorUserIdClaim.Value, out var actorUserId))
            {
                return Unauthorized("User identity could not be resolved.");
            }

            // Get tasks assigned to this assistant
            var tasks = await _chapterPageTaskService.GetAssignedTasksForAssistantAsync(actorUserId);

            return Ok(tasks);
        }

        /// <summary>
        /// Get detail of a specific task assigned to the current Assistant user.
        /// Assistant can only view tasks assigned to themselves.
        /// Returns 404 if task not found or not assigned to actor.
        /// </summary>
        /// <param name="taskId">The task ID.</param>
        /// <returns>Task detail with full context including page image and regions.</returns>
        [HttpGet("{taskId:guid}")]
        public async Task<ActionResult<ChapterPageTaskDto>> GetAssignedTaskDetailAsync(Guid taskId)
        {
            // Validate task ID
            if (taskId == Guid.Empty)
            {
                return BadRequest("Invalid task ID.");
            }

            // Get actor user ID from authenticated claims
            var actorUserIdClaim = User.FindFirst("sub") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (actorUserIdClaim == null || !Guid.TryParse(actorUserIdClaim.Value, out var actorUserId))
            {
                return Unauthorized("User identity could not be resolved.");
            }

            // Get task detail with ownership validation
            var task = await _chapterPageTaskService.GetAssignedTaskDetailForAssistantAsync(actorUserId, taskId);

            if (task == null)
            {
                return NotFound("Task not found or not assigned to current user.");
            }

            return Ok(task);
        }

        /// <summary>
        /// Submit assistant task work with file upload.
        /// Moves task from ASSIGNED → UNDER_REVIEW.
        /// </summary>
        /// <param name="taskId">The task ID.</param>
        /// <param name="file">The submission file (image or document).</param>
        /// <param name="versionNote">Optional notes about this version.</param>
        /// <returns>Submission result with task and version IDs.</returns>
        [HttpPost("{taskId:guid}/submit-work")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<AssistantTaskSubmitResultDto>> SubmitWorkAsync(
            [FromRoute] Guid taskId,
            IFormFile file,
            [FromForm] string? versionNote = null)
        {
            // Validate task ID
            if (taskId == Guid.Empty)
            {
                return BadRequest("Invalid task ID.");
            }

            // Validate file is present
            if (file == null || file.Length <= 0)
            {
                return BadRequest("A file is required for submission.");
            }

            // Validate file size (10 MB limit)
            const long MaxFileSizeBytes = 10 * 1024 * 1024;
            if (file.Length > MaxFileSizeBytes)
            {
                return BadRequest($"File exceeds maximum size of {MaxFileSizeBytes / 1024 / 1024} MB.");
            }

            // Validate file content type (images only for now)
            var allowedTypes = new[] { "image/png", "image/jpeg", "image/jpg", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType?.ToLowerInvariant()))
            {
                return BadRequest("Unsupported file type. Allowed: PNG, JPEG, WebP.");
            }

            // Get actor user ID from authenticated claims
            var actorUserIdClaim = User.FindFirst("sub") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (actorUserIdClaim == null || !Guid.TryParse(actorUserIdClaim.Value, out var actorUserId))
            {
                return Unauthorized("User identity could not be resolved.");
            }

            // Upload file to Cloudinary
            FileUploadResultDto uploadResult;
            try
            {
                // Note: uploadedBy parameter is int? in CloudinaryFileStorageFormAdapter
                // but actorUserId is Guid - pass null for now as optional parameter.
                uploadResult = await _formAdapter.UploadFormFileAsync(file, "CHAPTER_PAGE_VERSION", null);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest($"File upload failed: {ex.Message}");
            }

            // Build request DTO
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

            // Submit to service
            AssistantTaskSubmitResultDto? result;
            try
            {
                result = await _submissionService.SubmitTaskWorkAsync(request);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest($"Submission failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }

            if (result == null)
            {
                return NotFound("Task submission result could not be retrieved.");
            }

            return Ok(result);
        }
    }
}
