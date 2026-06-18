using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Features.Auth.Registration;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MangaManagementSystem.API.Controllers
{
    /// <summary>
    /// HTTP boundary for public registration workflows.
    /// Commands are dispatched through MediatR.
    /// </summary>
    [ApiController]
    [Route("api/registration")]
    public sealed class RegistrationController
        : ControllerBase
    {
        private readonly ISender _sender;
        private readonly ILogger<RegistrationController>
            _logger;

        public RegistrationController(
            ISender sender,
            ILogger<RegistrationController> logger)
        {
            _sender = sender;
            _logger = logger;
        }

        [HttpPost("otp")]
        public async Task<IActionResult> SendOtpAsync(
            [FromBody] SendRegistrationOtpRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(
                    new ApiErrorResponse(
                        AuthErrorCodes.ValidationFailed,
                        "Registration information is invalid."));
            }

            var command =
                new SendRegistrationOtpCommand(
                    request.Username,
                    request.Email,
                    request.Password,
                    request.RoleName,
                    request.DisplayName);

            try
            {
                await _sender.Send(
                    command,
                    cancellationToken);

                return Ok(
                    new ApiMessageResponse(
                        "A verification code has been sent to your email."));
            }
            catch (InvalidOperationException ex)
            {
                var errorCode =
                    ResolveRegistrationErrorCode(
                        ex.Message);

                return Conflict(
                    new ApiErrorResponse(
                        errorCode,
                        ResolveRegistrationMessage(
                            errorCode)));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error sending registration OTP.");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new ApiErrorResponse(
                        AuthErrorCodes.RegistrationStartFailed,
                        "We could not start registration right now. Please try again later."));
            }
        }

        [HttpPost("complete")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CompleteAsync(
            [FromForm] string email,
            [FromForm] string otp,
            IFormFile? portfolioFile = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email)
                || string.IsNullOrWhiteSpace(otp))
            {
                return BadRequest(
                    new ApiErrorResponse(
                        AuthErrorCodes.InvalidOtp,
                        "Email and OTP are required."));
            }

            byte[]? fileBytes = null;
            string? fileName = null;
            string? contentType = null;

            if (portfolioFile is { Length: > 0 })
            {
                await using var memoryStream =
                    new MemoryStream();

                await portfolioFile.CopyToAsync(
                    memoryStream,
                    cancellationToken);

                fileBytes =
                    memoryStream.ToArray();

                fileName =
                    portfolioFile.FileName;

                contentType =
                    portfolioFile.ContentType;
            }

            try
            {
                var user =
                    await _sender.Send(
                        new CompleteRegistrationCommand(
                            email,
                            otp,
                            fileBytes,
                            fileName,
                            contentType),
                        cancellationToken);

                return Ok(user);
            }
            catch (InvalidOperationException ex)
            {
                var errorCode =
                    ex.Message.Contains(
                        "verification code",
                        StringComparison.OrdinalIgnoreCase)
                        ? AuthErrorCodes.InvalidOtp
                        : ResolveRegistrationErrorCode(
                            ex.Message);

                return BadRequest(
                    new ApiErrorResponse(
                        errorCode,
                        ResolveRegistrationMessage(
                            errorCode)));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error completing registration.");

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new ApiErrorResponse(
                        AuthErrorCodes.RegistrationCompleteFailed,
                        "We could not complete registration right now. Please try again later."));
            }
        }

        private static string ResolveRegistrationErrorCode(
            string message)
        {
            if (message.Contains(
                    "email",
                    StringComparison.OrdinalIgnoreCase)
                && message.Contains(
                    "already",
                    StringComparison.OrdinalIgnoreCase))
            {
                return AuthErrorCodes.EmailAlreadyExists;
            }

            if (message.Contains(
                    "username",
                    StringComparison.OrdinalIgnoreCase))
            {
                return AuthErrorCodes.UsernameTaken;
            }

            if (message.Contains(
                    "role",
                    StringComparison.OrdinalIgnoreCase))
            {
                return AuthErrorCodes.InvalidRole;
            }

            return AuthErrorCodes.RegistrationStartFailed;
        }

        private static string ResolveRegistrationMessage(
            string errorCode)
        {
            return errorCode switch
            {
                AuthErrorCodes.InvalidOtp =>
                    "The verification code is invalid or has expired.",

                AuthErrorCodes.EmailAlreadyExists =>
                    "An account with this email already exists.",

                AuthErrorCodes.UsernameTaken =>
                    "This username is already taken.",

                AuthErrorCodes.InvalidRole =>
                    "The selected registration role is invalid.",

                _ =>
                    "The registration request could not be completed."
            };
        }
    }
}
