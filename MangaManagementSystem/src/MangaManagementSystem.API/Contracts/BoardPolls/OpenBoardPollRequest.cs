namespace MangaManagementSystem.API.Contracts.BoardPolls;

public sealed class OpenBoardPollRequest
{
    public Guid SeriesId { get; set; }
    public string PollTypeCode { get; set; } = "START_SERIALIZATION";
    public string PollReason { get; set; } = string.Empty;
    public string? BoardPublicationFrequencyCode { get; set; }
    public DateTime? EndsAtUtc { get; set; }
}