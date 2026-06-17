using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Auth.Registration
{
    public sealed class SendRegistrationOtpCommandHandler
        : IRequestHandler<SendRegistrationOtpCommand, bool>
    {
        private readonly IAuthService _authService;

        public SendRegistrationOtpCommandHandler(
            IAuthService authService)
        {
            _authService = authService;
        }

        public async Task<bool> Handle(
            SendRegistrationOtpCommand request,
            CancellationToken cancellationToken)
        {
            var normalizedRoleName =
                PublicRegistrationRoles.NormalizeOrThrow(
                    request.RoleName);

            var registerDto = new RegisterDto(
                request.Username,
                request.Email,
                request.Password,
                normalizedRoleName,
                request.DisplayName);

            return await _authService.SendRegistrationOtpAsync(
                registerDto);
        }
    }
}
