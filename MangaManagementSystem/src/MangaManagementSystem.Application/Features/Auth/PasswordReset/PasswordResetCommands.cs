using MediatR;

namespace MangaManagementSystem.Application.Features.Auth.PasswordReset
{
    public sealed record RequestPasswordResetCommand(
        string Email,
        string ResetPageUrl)
        : IRequest<bool>;

    public sealed record CompletePasswordResetCommand(
        string Token,
        string NewPassword)
        : IRequest<bool>;
}
