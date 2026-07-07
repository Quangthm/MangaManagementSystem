using MediatR;

namespace MangaManagementSystem.Application.Features.Auth.Registration
{
    public sealed record SendRegistrationOtpCommand(
        string Username,
        string Email,
        string Password,
        string RoleName,
        string? DisplayName
    ) : IRequest<bool>;
}
