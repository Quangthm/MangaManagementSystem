using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.Application.Common;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Features.Auth.Registration;
using MediatR;
using Microsoft.AspNetCore.Mvc;

<<<<<<< HEAD
namespace MangaManagementSystem.API.Controllers;

[ApiController]
[Route("api/registration")]
public sealed class RegistrationController : ControllerBase
=======
namespace MangaManagementSystem.API.Controllers
{
    [ApiController]
    [Route("api/registration")]
    public sealed class RegistrationController
        : ControllerBase
>>>>>>> main
    {
        private const long MultipartRequestLimitBytes =
            RegistrationPortfolioFileValidator.MaxFileSizeBytes
            + (1024L * 1024L);

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
        [RequestSizeLimit(MultipartRequestLimitBytes)]
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

            try
            {
                if (portfolioFile is not null)
                {
                    if (portfolioFile.Length <= 0)
                    {
                        throw new RegistrationPortfolioValidationException(
                            AuthErrorCodes.InvalidPortfolioFile,
                            "The portfolio file is empty.");
                    }

                    if (portfolioFile.Length >
                        RegistrationPortfolioFileValidator
                            .MaxFileSizeBytes)
                    {
                        throw new RegistrationPortfolioValidationException(
                            AuthErrorCodes.PortfolioFileTooLarge,
                            "The portfolio file is too large. The maximum size is 10 MB.");
                    }

                    await using var memoryStream =
                        new MemoryStream(
                            checked((int)portfolioFile.Length));

                    await portfolioFile.CopyToAsync(
                        memoryStream,
                        cancellationToken);

                    var validated =
                        RegistrationPortfolioFileValidator.Validate(
                            memoryStream.ToArray(),
                            portfolioFile.FileName,
                            portfolioFile.ContentType);

                    fileBytes = validated.Bytes;
                    fileName = validated.FileName;
                    contentType = validated.ContentType;
                }

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
            catch (RegistrationPortfolioValidationException ex)
            {
                var statusCode =
                    ex.ErrorCode ==
                    AuthErrorCodes.PortfolioFileTooLarge
                        ? StatusCodes.Status413PayloadTooLarge
                        : StatusCodes.Status400BadRequest;

                return StatusCode(
                    statusCode,
                    new ApiErrorResponse(
                        ex.ErrorCode,
                        ex.Message));
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
