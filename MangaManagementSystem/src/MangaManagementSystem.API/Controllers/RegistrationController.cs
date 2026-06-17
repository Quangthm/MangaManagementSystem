using MangaManagementSystem.API.Contracts;
using MangaManagementSystem.Application.Features.Auth.Registration;
using MangaManagementSystem.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MangaManagementSystem.API.Controllers
{
    /// <summary>
    /// HTTP boundary for the public registration workflow.
    /// Commands are dispatched through MediatR.
    /// </summary>
    [ApiController]
    [Route("api/registration")]
    public class RegistrationController : ControllerBase
    {
        private readonly ISender _sender;
        private readonly IAuthService _authService;
        private readonly ILogger<RegistrationController> _logger;

        public RegistrationController(
            ISender sender,
            IAuthService authService,
            ILogger<RegistrationController> logger)
        {
            _sender = sender;
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Step 1: validate the public role and send an OTP to the email.
        /// </summary>
        [HttpPost("otp")]
        public async Task<IActionResult> SendOtpAsync(
            [FromBody] SendRegistrationOtpRequest request,
            CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var command = new SendRegistrationOtpCommand(
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
                return Conflict(
                    new ApiErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error sending registration OTP.");

                return Problem(
                    detail:
                        "We could not start registration right now. Please try again later.",
                    statusCode:
                        StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Step 2: complete registration using the emailed OTP code.
        /// Accepts multipart/form-data so an optional portfolio file can be
        /// uploaded alongside the email and OTP fields.
        /// </summary>
        [HttpPost("complete")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CompleteAsync(
            [FromForm] string email,
            [FromForm] string otp,
            IFormFile? portfolioFile = null)
        {
            if (string.IsNullOrWhiteSpace(email)
                || string.IsNullOrWhiteSpace(otp))
            {
                return BadRequest(
                    new ApiErrorResponse(
                        "Email and OTP are required."));
            }

            byte[]? fileBytes = null;
            string? fileName = null;
            string? contentType = null;

            if (portfolioFile is { Length: > 0 })
            {
                using var memoryStream =
                    new MemoryStream();

                await portfolioFile.CopyToAsync(
                    memoryStream);

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
                    await _authService
                        .CompleteRegistrationWithOtpAsync(
                            email,
                            otp,
                            fileBytes,
                            fileName,
                            contentType);

                return Ok(user);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(
                    new ApiErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error completing registration.");

                return Problem(
                    detail:
                        "We could not complete registration right now. Please try again later.",
                    statusCode:
                        StatusCodes.Status500InternalServerError);
            }
        }
    }
}
