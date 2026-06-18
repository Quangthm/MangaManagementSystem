using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace MangaManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/profile")]
    public sealed class ProfileController : ControllerBase
    {
        private const string AvatarPurposeCode =
            "USER_AVATAR";

        private const string PortfolioPurposeCode =
            "REGISTRATION_PORTFOLIO";

        private readonly IUserService _userService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IFileResourceService _fileResourceService;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            IUserService userService,
            IFileStorageService fileStorageService,
            IFileResourceService fileResourceService,
            ILogger<ProfileController> logger)
        {
            _userService = userService;
            _fileStorageService = fileStorageService;
            _fileResourceService = fileResourceService;
            _logger = logger;
        }

        [HttpGet("{userId:guid}")]
        public async Task<IActionResult> GetProfileAsync(
            Guid userId)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest(
                    new ProfileMessageResponse(
                        "User id is required."));
            }

            var user =
                await _userService.GetUserByIdAsync(userId);

            if (user == null)
            {
                return NotFound(
                    new ProfileMessageResponse(
                        "User was not found."));
            }

            return Ok(user);
        }

        [HttpGet("files/{fileResourceId:guid}")]
        public async Task<IActionResult> GetFileAsync(
            Guid fileResourceId)
        {
            if (fileResourceId == Guid.Empty)
            {
                return BadRequest(
                    new ProfileMessageResponse(
                        "File resource id is required."));
            }

            var file =
                await _fileResourceService
                    .GetFileResourceByIdAsync(
                        fileResourceId);

            if (file == null ||
                file.DeletedAtUtc != null)
            {
                return NotFound(
                    new ProfileMessageResponse(
                        "File resource was not found."));
            }

            return Ok(file);
        }

        [HttpPut("{userId:guid}/display-name")]
        public async Task<IActionResult>
            UpdateDisplayNameAsync(
                Guid userId,
                [FromBody]
                UpdateProfileDisplayNameRequest request)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest(
                    new ProfileMessageResponse(
                        "User id is required."));
            }

            if (string.IsNullOrWhiteSpace(
                    request.DisplayName))
            {
                return BadRequest(
                    new ProfileMessageResponse(
                        "Display name cannot be empty."));
            }

            try
            {
                var updated =
                    await _userService
                        .UpdateDisplayNameAsync(
                            userId,
                            request.DisplayName);

                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new ProfileMessageResponse(
                        ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to update display name for user {UserId}.",
                    userId);

                return Problem(
                    detail:
                        "The display name could not be updated.",
                    statusCode:
                        StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("{userId:guid}/avatar")]
        public async Task<IActionResult>
            UpdateAvatarAsync(
                Guid userId,
                IFormFile file)
        {
            return await UpdateFileAsync(
                userId,
                file,
                AvatarPurposeCode,
                updateAvatar: true);
        }

        [HttpPost("{userId:guid}/portfolio")]
        public async Task<IActionResult>
            UpdatePortfolioAsync(
                Guid userId,
                IFormFile file)
        {
            return await UpdateFileAsync(
                userId,
                file,
                PortfolioPurposeCode,
                updateAvatar: false);
        }

        private async Task<IActionResult> UpdateFileAsync(
            Guid userId,
            IFormFile file,
            string filePurposeCode,
            bool updateAvatar)
        {
            if (userId == Guid.Empty)
            {
                return BadRequest(
                    new ProfileMessageResponse(
                        "User id is required."));
            }

            if (file == null ||
                file.Length <= 0)
            {
                return BadRequest(
                    new ProfileMessageResponse(
                        "A valid file is required."));
            }

            try
            {
                await using var stream =
                    new MemoryStream();

                await file.CopyToAsync(stream);

                var originalFileName =
                    Path.GetFileName(file.FileName);

                var contentType =
                    string.IsNullOrWhiteSpace(
                        file.ContentType)
                        ? "application/octet-stream"
                        : file.ContentType;

                var upload =
                    await _fileStorageService
                        .UploadFileAsync(
                            stream.ToArray(),
                            originalFileName,
                            contentType,
                            filePurposeCode);

                var updated =
                    updateAvatar
                        ? await _userService
                            .UpdateAvatarFileAsync(
                                userId,
                                upload)
                        : await _userService
                            .UpdatePortfolioFileAsync(
                                userId,
                                upload);

                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new ProfileMessageResponse(
                        ex.Message));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(
                    new ProfileMessageResponse(
                        ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to update {ProfileFileType} for user {UserId}.",
                    updateAvatar
                        ? "avatar"
                        : "portfolio",
                    userId);

                return Problem(
                    detail:
                        updateAvatar
                            ? "The avatar could not be updated."
                            : "The portfolio could not be updated.",
                    statusCode:
                        StatusCodes.Status500InternalServerError);
            }
        }
    }
}
