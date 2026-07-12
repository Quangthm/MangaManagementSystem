using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Features.Auth.PasswordReset;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        public PasswordResetController(
            ISender sender,
            ILogger<PasswordResetController> logger)
        {
            _sender = sender;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost("request")]
        public async Task<IActionResult> RequestAsync(
            [FromBody] RequestPasswordResetRequest request,
            CancellationToken cancellationToken)
        {
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

        [AllowAnonymous]
        [HttpPost("complete")]
        public async Task<IActionResult> CompleteAsync(
            [FromBody] CompletePasswordResetRequest request,
            CancellationToken cancellationToken)
        {
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
    }
}
