using System.Security.Cryptography;
using System.Text;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.API.Options;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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

        private const string ActiveStatusCode =
            "ACTIVE";

        private readonly IUserService _userService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IFileResourceService _fileResourceService;
        private readonly ILogger<ProfileController> _logger;
        private readonly InternalApiOptions _internalApiOptions;

        public ProfileController(
            IUserService userService,
            IFileStorageService fileStorageService,
            IFileResourceService fileResourceService,
            ILogger<ProfileController> logger,
            IOptions<InternalApiOptions> internalApiOptions)
        {
            _userService = userService;
            _fileStorageService = fileStorageService;
            _fileResourceService = fileResourceService;
            _logger = logger;
            _internalApiOptions = internalApiOptions.Value;
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

            var authorization =
                await ResolveAuthorizedActorAsync();

            if (authorization.Error is not null)
            {
                return authorization.Error;
            }

            if (authorization.Actor!.UserId != userId)
            {
                return ProfileAccessDenied();
            }

            var user =
                await _userService.GetUserByIdAsync(
                    userId);

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

            var authorization =
                await ResolveAuthorizedActorAsync();

            if (authorization.Error is not null)
            {
                return authorization.Error;
            }

            var actor =
                authorization.Actor!;

            var isOwnedProfileFile =
                actor.AvatarFileId == fileResourceId
                || actor.PortfolioFileId == fileResourceId;

            if (!isOwnedProfileFile)
            {
                _logger.LogWarning(
                    "User {ActorUserId} attempted to access profile file {FileResourceId} owned by another account.",
                    actor.UserId,
                    fileResourceId);

                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new ProfileMessageResponse(
                        "You do not have permission to access this file."));
            }

            var file =
                await _fileResourceService
                    .GetFileResourceByIdAsync(
                        fileResourceId);

            if (file == null
                || file.DeletedAtUtc != null)
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

            var authorization =
                await ResolveAuthorizedActorAsync();

            if (authorization.Error is not null)
            {
                return authorization.Error;
            }

            if (authorization.Actor!.UserId != userId)
            {
                return ProfileAccessDenied();
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
            var authorization =
                await ResolveAuthorizedActorAsync();

            if (authorization.Error is not null)
            {
                return authorization.Error;
            }

            if (authorization.Actor!.UserId != userId)
            {
                return ProfileAccessDenied();
            }

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
            var authorization =
                await ResolveAuthorizedActorAsync();

            if (authorization.Error is not null)
            {
                return authorization.Error;
            }

            if (authorization.Actor!.UserId != userId)
            {
                return ProfileAccessDenied();
            }

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

            if (file == null
                || file.Length <= 0)
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
                    Path.GetFileName(
                        file.FileName);

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

        private async Task<(
            UserDto? Actor,
            IActionResult? Error)>
            ResolveAuthorizedActorAsync()
        {
            if (!Request.Headers.TryGetValue(
                    InternalApiOptions.HeaderName,
                    out var suppliedKey)
                || !KeysMatch(
                    suppliedKey.ToString(),
                    _internalApiOptions.Key))
            {
                _logger.LogWarning(
                    "Rejected unauthorized internal profile request.");

                return (
                    null,
                    Unauthorized(
                        new ProfileMessageResponse(
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
                        new ProfileMessageResponse(
                            "Authenticated actor information is invalid.")));
            }

            if (!Request.Headers.TryGetValue(
                    InternalApiOptions.ActorRoleHeaderName,
                    out var actorRoleHeader)
                || string.IsNullOrWhiteSpace(
                    actorRoleHeader.ToString()))
            {
                return (
                    null,
                    Unauthorized(
                        new ProfileMessageResponse(
                            "Authenticated actor role is unavailable.")));
            }

            var actor =
                await _userService.GetUserByIdAsync(
                    actorUserId);

            if (actor is null)
            {
                return (
                    null,
                    Unauthorized(
                        new ProfileMessageResponse(
                            "Authenticated actor was not found.")));
            }

            if (!string.Equals(
                    actor.StatusCode,
                    ActiveStatusCode,
                    StringComparison.OrdinalIgnoreCase))
            {
                return (
                    null,
                    StatusCode(
                        StatusCodes.Status403Forbidden,
                        new ProfileMessageResponse(
                            "The account is not active.")));
            }

            if (!string.Equals(
                    actor.RoleName,
                    actorRoleHeader.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "Rejected profile request because supplied role {SuppliedRole} did not match stored role {StoredRole} for user {ActorUserId}.",
                    actorRoleHeader.ToString(),
                    actor.RoleName,
                    actor.UserId);

                return (
                    null,
                    Unauthorized(
                        new ProfileMessageResponse(
                            "Authenticated actor information is invalid.")));
            }

            return (
                actor,
                null);
        }

        private IActionResult ProfileAccessDenied()
        {
            _logger.LogWarning(
                "User attempted to access or modify another account's profile.");

            return StatusCode(
                StatusCodes.Status403Forbidden,
                new ProfileMessageResponse(
                    "You may only manage your own profile."));
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
