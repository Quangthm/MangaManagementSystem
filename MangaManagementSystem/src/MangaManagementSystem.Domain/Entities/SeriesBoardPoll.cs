using System;
using System.Collections.Generic;

namespace MangaManagementSystem.Domain.Entities
{
public class SeriesBoardPoll
{
    public Guid SeriesBoardPollId { get; set; }
    public Guid SeriesId { get; set; }
    public Series? Series { get; set; }
    public string PollTypeCode { get; set; } = null!;
    public string PollReason { get; set; } = null!;
    public string PollStatusCode { get; set; } = "OPEN";
    public Guid CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public ICollection<SeriesBoardVote> Votes { get; set; } = new List<SeriesBoardVote>();
}
}
