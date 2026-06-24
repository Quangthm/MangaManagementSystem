using System.Net;
using System.Security.Cryptography;
using System.Text;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.API.Options;
using MangaManagementSystem.Application.DTOs.Admin;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Features.Admin.Files.Commands;
using MangaManagementSystem.Application.Features.Admin.Files.Queries;
using MangaManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace MangaManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/admin/files")]
    public sealed class AdminFilesController
        : ControllerBase
    {
        public const string ContentHttpClientName =
            "AdminFileContent";

        private const string ActiveStatusCode =
            "ACTIVE";

        private const string AdminRoleName =
            "Admin";

        private const long MaximumProxyBytes =
            10L * 1024L * 1024L;

        private static class AdminFileErrorCodes
        {
            internal const string InvalidRequest =
                "ADMIN_FILE_INVALID_REQUEST";

            internal const string AccessDenied =
                "ADMIN_FILE_ACCESS_DENIED";

            internal const string NotFound =
                "ADMIN_FILE_NOT_FOUND";

            internal const string AlreadyDeleted =
                "ADMIN_FILE_ALREADY_DELETED";

            internal const string Deleted =
                "ADMIN_FILE_DELETED";

            internal const string PreviewUnsupported =
                "ADMIN_FILE_PREVIEW_UNSUPPORTED";

            internal const string StorageUnavailable =
                "ADMIN_FILE_STORAGE_UNAVAILABLE";

            internal const string TooLarge =
                "ADMIN_FILE_TOO_LARGE";

            internal const string RequestFailed =
                "ADMIN_FILE_REQUEST_FAILED";
        }

        private readonly ISender _sender;
        private readonly IUserService _userService;
        private readonly IHttpClientFactory
            _httpClientFactory;
        private readonly ILogger<AdminFilesController>
            _logger;
        private readonly InternalApiOptions
            _internalApiOptions;

        public AdminFilesController(
            ISender sender,
            IUserService userService,
            IHttpClientFactory httpClientFactory,
            ILogger<AdminFilesController> logger,
            IOptions<InternalApiOptions>
                internalApiOptions)
        {
            _sender = sender;
            _userService = userService;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _internalApiOptions =
                internalApiOptions.Value;
        }

        [HttpGet]
        public async Task<IActionResult> SearchAsync(
            [FromQuery] string? search,
            [FromQuery] string? filePurposeCode,
            [FromQuery] string? deletedState = "ACTIVE",
            [FromQuery] DateTime? fromUtc = null,
            [FromQuery] DateTime? toUtc = null,
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
                        new SearchAdminFilesQuery(
                            authorization.Actor!.UserId,
                            search,
                            filePurposeCode,
                            deletedState,
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
                        AdminFileErrorCodes.InvalidRequest,
                        ex.Message));
            }
            catch (SqlException ex)
            {
                return MapSqlException(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to search Admin file resources.");

                return AdminFileRequestFailed();
            }
        }

        [HttpGet("{fileResourceId:guid}")]
        public async Task<IActionResult> GetByIdAsync(
            Guid fileResourceId,
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
                var detail =
                    await _sender.Send(
                        new GetAdminFileDetailQuery(
                            authorization.Actor!.UserId,
                            fileResourceId),
                        cancellationToken);

                if (detail is null)
                {
                    return FileNotFound();
                }

                return Ok(detail);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new ApiErrorResponse(
                        AdminFileErrorCodes.InvalidRequest,
                        ex.Message));
            }
            catch (SqlException ex)
            {
                return MapSqlException(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to load Admin file resource {FileResourceId}.",
                    fileResourceId);

                return AdminFileRequestFailed();
            }
        }

        [HttpPost("{fileResourceId:guid}/delete")]
        [HttpPost("{fileResourceId:guid}/soft-delete")]
        public async Task<IActionResult> DeleteAsync(
            Guid fileResourceId,
            [FromBody] AdminFileSoftDeleteRequest request,
            CancellationToken cancellationToken)
        {
            var authorization =
                await ResolveAdminActorAsync();

            if (authorization.Error is not null)
            {
                return authorization.Error;
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(
                    new ApiErrorResponse(
                        AdminFileErrorCodes.InvalidRequest,
                        "A deletion reason is required and cannot exceed 500 characters."));
            }

            try
            {
                var detail =
                    await _sender.Send(
                        new SoftDeleteAdminFileCommand(
                            authorization.Actor!.UserId,
                            fileResourceId,
                            request.DeleteReason),
                        cancellationToken);

                return Ok(detail);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new ApiErrorResponse(
                        AdminFileErrorCodes.InvalidRequest,
                        ex.Message));
            }
            catch (SqlException ex)
            {
                return MapSqlException(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to delete file resource {FileResourceId}.",
                    fileResourceId);

                return AdminFileRequestFailed();
            }
        }

        [HttpPost("{fileResourceId:guid}/cleanup")]
        public async Task<IActionResult> CleanupAsync(
            Guid fileResourceId,
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
                var result =
                    await _sender.Send(
                        new CleanupAdminFileStorageCommand(
                            authorization.Actor!.UserId,
                            fileResourceId),
                        cancellationToken);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new ApiErrorResponse(
                        AdminFileErrorCodes.InvalidRequest,
                        ex.Message));
            }
            catch (SqlException ex)
            {
                return MapSqlException(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to clean up storage for file resource {FileResourceId}.",
                    fileResourceId);

                return AdminFileRequestFailed();
            }
        }

        [HttpPost("cleanup-deleted")]
        public async Task<IActionResult> CleanupDeletedAsync(
            [FromQuery] int batchSize = 20,
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
                var result =
                    await _sender.Send(
                        new CleanupDeletedAdminFilesStorageCommand(
                            authorization.Actor!.UserId,
                            batchSize),
                        cancellationToken);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new ApiErrorResponse(
                        AdminFileErrorCodes.InvalidRequest,
                        ex.Message));
            }
            catch (SqlException ex)
            {
                return MapSqlException(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to clean up deleted file resources.");

                return AdminFileRequestFailed();
            }
        }

        [HttpGet("{fileResourceId:guid}/preview")]
        public Task<IActionResult> PreviewAsync(
            Guid fileResourceId,
            CancellationToken cancellationToken)
        {
            return GetControlledContentAsync(
                fileResourceId,
                asDownload: false,
                cancellationToken);
        }

        [HttpGet("{fileResourceId:guid}/download")]
        public Task<IActionResult> DownloadAsync(
            Guid fileResourceId,
            CancellationToken cancellationToken)
        {
            return GetControlledContentAsync(
                fileResourceId,
                asDownload: true,
                cancellationToken);
        }

        private async Task<IActionResult>
            GetControlledContentAsync(
                Guid fileResourceId,
                bool asDownload,
                CancellationToken cancellationToken)
        {
            var authorization =
                await ResolveAdminActorAsync();

            if (authorization.Error is not null)
            {
                return authorization.Error;
            }

            AdminFileContentSourceDto? source;

            try
            {
                source =
                    await _sender.Send(
                        new GetAdminFileContentSourceQuery(
                            authorization.Actor!.UserId,
                            fileResourceId),
                        cancellationToken);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new ApiErrorResponse(
                        AdminFileErrorCodes.InvalidRequest,
                        ex.Message));
            }
            catch (SqlException ex)
            {
                return MapSqlException(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to resolve controlled content for file resource {FileResourceId}.",
                    fileResourceId);

                return AdminFileRequestFailed();
            }

            if (source is null)
            {
                return asDownload
                    ? FileNotFound()
                    : SafePlaceholder(
                        "missing");
            }

            if (source.IsDeleted)
            {
                return asDownload
                    ? StatusCode(
                        StatusCodes.Status410Gone,
                        new ApiErrorResponse(
                            AdminFileErrorCodes.Deleted,
                            "The file has been deleted and is no longer available for download."))
                    : SafePlaceholder(
                        "deleted");
            }

            if (!asDownload
                && !source.CanPreview)
            {
                return StatusCode(
                    StatusCodes.Status415UnsupportedMediaType,
                    new ApiErrorResponse(
                        AdminFileErrorCodes.PreviewUnsupported,
                        "This file type cannot be previewed safely. Use the controlled download endpoint instead."));
            }

            if (source.FileSizeBytes > MaximumProxyBytes)
            {
                return StatusCode(
                    StatusCodes.Status413PayloadTooLarge,
                    new ApiErrorResponse(
                        AdminFileErrorCodes.TooLarge,
                        "The file is too large to be proxied safely."));
            }

            if (!TryCreateAllowedStorageUri(
                    source.CloudinarySecureUrl,
                    out var storageUri))
            {
                _logger.LogWarning(
                    "Rejected invalid storage locator for file resource {FileResourceId}.",
                    fileResourceId);

                return StorageUnavailable();
            }

            try
            {
                var remote =
                    await FetchRemoteFileAsync(
                        storageUri,
                        cancellationToken);

                if (remote.StatusCode ==
                    HttpStatusCode.NotFound)
                {
                    return asDownload
                        ? FileNotFound()
                        : SafePlaceholder(
                            "missing");
                }

                if (remote.StatusCode !=
                    HttpStatusCode.OK
                    || remote.Bytes is null)
                {
                    _logger.LogWarning(
                        "Storage returned {StatusCode} for file resource {FileResourceId}.",
                        (int)remote.StatusCode,
                        fileResourceId);

                    return StorageUnavailable();
                }

                var contentType =
                    NormalizeContentType(
                        source.ContentType);

                Response.Headers.CacheControl =
                    "no-store, no-cache, max-age=0";

                Response.Headers[
                    "X-Content-Type-Options"] =
                    "nosniff";

                if (asDownload)
                {
                    return File(
                        remote.Bytes,
                        contentType,
                        SanitizeFileName(
                            source.OriginalFileName));
                }

                Response.Headers[
                    "Content-Security-Policy"] =
                    "default-src 'none'; sandbox";

                return File(
                    remote.Bytes,
                    contentType);
            }
            catch (FileContentTooLargeException)
            {
                return StatusCode(
                    StatusCodes.Status413PayloadTooLarge,
                    new ApiErrorResponse(
                        AdminFileErrorCodes.TooLarge,
                        "The file is too large to be proxied safely."));
            }
            catch (OperationCanceledException)
                when (!cancellationToken.IsCancellationRequested)
            {
                return StatusCode(
                    StatusCodes.Status504GatewayTimeout,
                    new ApiErrorResponse(
                        AdminFileErrorCodes.StorageUnavailable,
                        "The file storage request timed out."));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Storage request failed for file resource {FileResourceId}.",
                    fileResourceId);

                return StorageUnavailable();
            }
        }

        private async Task<RemoteFileResult>
            FetchRemoteFileAsync(
                Uri storageUri,
                CancellationToken cancellationToken)
        {
            var client =
                _httpClientFactory.CreateClient(
                    ContentHttpClientName);

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    storageUri);

            using var response =
                await client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

            if (response.StatusCode !=
                HttpStatusCode.OK)
            {
                return new RemoteFileResult(
                    response.StatusCode,
                    null);
            }

            if (response.Content.Headers.ContentLength
                    is long contentLength
                && contentLength > MaximumProxyBytes)
            {
                throw new FileContentTooLargeException();
            }

            await using var remoteStream =
                await response.Content.ReadAsStreamAsync(
                    cancellationToken);

            using var memoryStream =
                new MemoryStream();

            var buffer =
                new byte[81920];

            long totalBytes = 0;

            while (true)
            {
                var bytesRead =
                    await remoteStream.ReadAsync(
                        buffer.AsMemory(
                            0,
                            buffer.Length),
                        cancellationToken);

                if (bytesRead == 0)
                {
                    break;
                }

                totalBytes += bytesRead;

                if (totalBytes > MaximumProxyBytes)
                {
                    throw new FileContentTooLargeException();
                }

                await memoryStream.WriteAsync(
                    buffer.AsMemory(
                        0,
                        bytesRead),
                    cancellationToken);
            }

            return new RemoteFileResult(
                response.StatusCode,
                memoryStream.ToArray());
        }

        private IActionResult MapSqlException(
            SqlException exception)
        {
            return exception.Number switch
            {
                54001 =>
                    FileNotFound(),

                54002 =>
                    Conflict(
                        new ApiErrorResponse(
                            AdminFileErrorCodes.AlreadyDeleted,
                            "The file resource is already deleted.")),

                54011 or 54022 or 54033 =>
                    StatusCode(
                        StatusCodes.Status403Forbidden,
                        new ApiErrorResponse(
                            AdminFileErrorCodes.AccessDenied,
                            "The current account is not an active administrator.")),

                54010 or 54012 or 54013 or 54014
                    or 54015 or 54020 or 54021
                    or 54030 or 54031 or 54032
                    or 54034 =>
                    BadRequest(
                        new ApiErrorResponse(
                            AdminFileErrorCodes.InvalidRequest,
                            exception.Message)),

                _ =>
                    LogAndCreateSqlFailure(
                        exception)
            };
        }

        private IActionResult LogAndCreateSqlFailure(
            SqlException exception)
        {
            _logger.LogError(
                exception,
                "Admin File Management database request failed with SQL error {SqlErrorNumber}.",
                exception.Number);

            return AdminFileRequestFailed();
        }

        private IActionResult FileNotFound()
        {
            return NotFound(
                new ApiErrorResponse(
                    AdminFileErrorCodes.NotFound,
                    "The file resource was not found."));
        }

        private IActionResult StorageUnavailable()
        {
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new ApiErrorResponse(
                    AdminFileErrorCodes.StorageUnavailable,
                    "The file content is currently unavailable."));
        }

        private IActionResult AdminFileRequestFailed()
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new ApiErrorResponse(
                    AdminFileErrorCodes.RequestFailed,
                    "The Admin File Management request could not be completed."));
        }

        private IActionResult SafePlaceholder(
            string reason)
        {
            var normalizedReason =
                string.Equals(
                    reason,
                    "deleted",
                    StringComparison.OrdinalIgnoreCase)
                    ? "deleted"
                    : "missing";

            const string svg =
                "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"640\" height=\"360\" viewBox=\"0 0 640 360\">"
                + "<rect width=\"640\" height=\"360\" fill=\"#f3f4f6\"/>"
                + "<rect x=\"190\" y=\"78\" width=\"260\" height=\"204\" rx=\"16\" fill=\"#ffffff\" stroke=\"#9ca3af\" stroke-width=\"4\"/>"
                + "<path d=\"M240 222l58-60 45 45 31-31 46 46\" fill=\"none\" stroke=\"#9ca3af\" stroke-width=\"10\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>"
                + "<circle cx=\"278\" cy=\"134\" r=\"18\" fill=\"#9ca3af\"/>"
                + "<text x=\"320\" y=\"320\" text-anchor=\"middle\" font-family=\"Arial, sans-serif\" font-size=\"22\" fill=\"#4b5563\">File unavailable</text>"
                + "</svg>";

            Response.Headers.CacheControl =
                "no-store, no-cache, max-age=0";

            Response.Headers[
                "X-Content-Type-Options"] =
                "nosniff";

            Response.Headers[
                "Content-Security-Policy"] =
                "default-src 'none'; sandbox";

            Response.Headers[
                "X-File-Placeholder"] =
                normalizedReason;

            return File(
                Encoding.UTF8.GetBytes(svg),
                "image/svg+xml");
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
                    "Rejected unauthorized internal Admin file request.");

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
                            AdminFileErrorCodes.AccessDenied,
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
                            AdminFileErrorCodes.AccessDenied,
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
                            AdminFileErrorCodes.AccessDenied,
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
                            AdminFileErrorCodes.AccessDenied,
                            "The current account is not an active administrator.")));
            }

            return (
                actor,
                null);
        }

        private static bool TryCreateAllowedStorageUri(
            string secureUrl,
            out Uri storageUri)
        {
            storageUri = null!;

            if (!Uri.TryCreate(
                    secureUrl,
                    UriKind.Absolute,
                    out var candidate)
                || candidate.Scheme != Uri.UriSchemeHttps
                || (!candidate.IsDefaultPort
                    && candidate.Port != 443)
                || !IsAllowedCloudinaryHost(
                    candidate.Host))
            {
                return false;
            }

            storageUri = candidate;
            return true;
        }

        private static bool IsAllowedCloudinaryHost(
            string host)
        {
            return string.Equals(
                    host,
                    "res.cloudinary.com",
                    StringComparison.OrdinalIgnoreCase)
                || host.EndsWith(
                    ".cloudinary.com",
                    StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeContentType(
            string contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType)
                || contentType.Contains('\r')
                || contentType.Contains('\n'))
            {
                return "application/octet-stream";
            }

            return contentType.Trim();
        }

        private static string SanitizeFileName(
            string originalFileName)
        {
            var fileName =
                Path.GetFileName(
                    originalFileName ?? string.Empty)
                    .Replace("\r", string.Empty)
                    .Replace("\n", string.Empty)
                    .Replace("\"", string.Empty)
                    .Trim();

            return string.IsNullOrWhiteSpace(fileName)
                ? "download"
                : fileName;
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

        private sealed record RemoteFileResult(
            HttpStatusCode StatusCode,
            byte[]? Bytes);

        private sealed class FileContentTooLargeException
            : Exception;
    }
}
