namespace MangaManagementSystem.API.Contracts.BoardPolls;

public sealed class UpdateBoardPollDeadlineRequest
{
    public DateTime? EndsAtUtc { get; set; }
}