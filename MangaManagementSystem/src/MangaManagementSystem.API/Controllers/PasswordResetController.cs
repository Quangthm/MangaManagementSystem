using System.Security.Cryptography;
using System.Text;
using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.API.Options;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Features.Auth.PasswordReset;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MangaManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/password-reset")]
    public sealed class PasswordResetController
        : ControllerBase
    {
        private const string GenericRequestMessage =
            "If an account exists for that email, a password reset link has been sent.";

        private readonly ISender _sender;
        private readonly ILogger<PasswordResetController>
            _logger;
        private readonly InternalApiOptions
            _internalApiOptions;

        public PasswordResetController(
            ISender sender,
            ILogger<PasswordResetController> logger,
            IOptions<InternalApiOptions> internalApiOptions)
        {
            _sender = sender;
            _logger = logger;
            _internalApiOptions =
                internalApiOptions.Value;
        }

        [HttpPost("request")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> RequestAsync(
            [FromBody] RequestPasswordResetRequest request,
            CancellationToken cancellationToken)
        {
            if (!HasValidInternalApiKey())
            {
                return InternalUnauthorized();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(
                    new ApiErrorResponse(
                        AuthErrorCodes.ValidationFailed,
                        "A valid email address and reset page URL are required."));
            }

            try
            {
                await _sender.Send(
                    new RequestPasswordResetCommand(
                        request.Email,
                        request.ResetPageUrl),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Password reset request processing failed.");
            }

            return Ok(
                new ApiMessageResponse(
                    GenericRequestMessage));
        }

        [HttpPost("complete")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> CompleteAsync(
            [FromBody] CompletePasswordResetRequest request,
            CancellationToken cancellationToken)
        {
            if (!HasValidInternalApiKey())
            {
                return InternalUnauthorized();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(
                    new ApiErrorResponse(
                        AuthErrorCodes.ValidationFailed,
                        "A valid reset token and password are required."));
            }

            try
            {
                await _sender.Send(
                    new CompletePasswordResetCommand(
                        request.Token,
                        request.NewPassword),
                    cancellationToken);

                return Ok(
                    new ApiMessageResponse(
                        "Password reset successfully."));
            }
            catch (InvalidOperationException)
            {
                return BadRequest(
                    new ApiErrorResponse(
                        AuthErrorCodes.InvalidPasswordResetToken,
                        "The password reset link is invalid or has expired."));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Password reset completion failed.");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new ApiErrorResponse(
                        AuthErrorCodes.PasswordResetCompleteFailed,
                        "The password could not be reset right now. Please request a new link."));
            }
        }

        private IActionResult InternalUnauthorized()
        {
            _logger.LogWarning(
                "Rejected unauthorized internal password-reset request.");

            return Unauthorized(
                new ApiErrorResponse(
                    AuthErrorCodes.UnauthorizedInternalRequest,
                    "Unauthorized internal request."));
        }

        private bool HasValidInternalApiKey()
        {
            return Request.Headers.TryGetValue(
                    InternalApiOptions.HeaderName,
                    out var suppliedKey)
                && KeysMatch(
                    suppliedKey.ToString(),
                    _internalApiOptions.Key);
        }

        private static bool KeysMatch(
            string suppliedKey,
            string expectedKey)
        {
            if (string.IsNullOrWhiteSpace(suppliedKey)
                || string.IsNullOrWhiteSpace(expectedKey))
            {
                return false;
            }

            var suppliedBytes =
                Encoding.UTF8.GetBytes(
                    suppliedKey);

            var expectedBytes =
                Encoding.UTF8.GetBytes(
                    expectedKey);

            return suppliedBytes.Length == expectedBytes.Length
                && CryptographicOperations.FixedTimeEquals(
                    suppliedBytes,
                    expectedBytes);
        }
    }
}
