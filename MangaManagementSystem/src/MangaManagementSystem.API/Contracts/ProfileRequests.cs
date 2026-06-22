namespace MangaManagementSystem.API.Contracts
{
    public sealed record UpdateProfileDisplayNameRequest(
        string DisplayName
    );

    public sealed record ProfileMessageResponse(
        string Message
    );
}
