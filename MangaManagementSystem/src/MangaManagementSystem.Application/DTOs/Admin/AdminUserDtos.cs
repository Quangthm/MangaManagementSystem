namespace MangaManagementSystem.Application.DTOs.Admin
{
    public sealed record AdminUserStatusCountsDto(
        int PendingApproval,
        int Active,
        int Disabled,
        int Rejected,
        int Total);
}
