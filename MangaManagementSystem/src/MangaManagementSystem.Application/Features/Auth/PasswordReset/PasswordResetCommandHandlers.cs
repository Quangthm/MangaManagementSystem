using MangaManagementSystem.Application.Interfaces;
using MediatR;

namespace MangaManagementSystem.Application.Features.Auth.PasswordReset
{
    public sealed class RequestPasswordResetCommandHandler
        : IRequestHandler<
            RequestPasswordResetCommand,
            bool>
    {
        private readonly IAuthService _authService;

        public RequestPasswordResetCommandHandler(
            IAuthService authService)
        {
            _authService = authService;
        }

        public async Task<bool> Handle(
            RequestPasswordResetCommand request,
            CancellationToken cancellationToken)
        {
            await _authService.RequestPasswordResetAsync(
                request.Email,
                request.ResetPageUrl,
                cancellationToken);

            return true;
        }
    }

    public sealed class CompletePasswordResetCommandHandler
        : IRequestHandler<
            CompletePasswordResetCommand,
            bool>
    {
        private readonly IAuthService _authService;

        public CompletePasswordResetCommandHandler(
            IAuthService authService)
        {
            _authService = authService;
        }

        public async Task<bool> Handle(
            CompletePasswordResetCommand request,
            CancellationToken cancellationToken)
        {
            await _authService.ResetPasswordWithTokenAsync(
                request.Token,
                request.NewPassword,
                cancellationToken);

            return true;
        }
    }
}
