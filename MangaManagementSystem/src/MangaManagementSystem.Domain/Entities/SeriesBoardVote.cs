using System;

namespace MangaManagementSystem.Domain.Entities
{
public class SeriesBoardVote
{
    public Guid SeriesBoardVoteId { get; set; }
    public Guid SeriesBoardPollId { get; set; }
    public SeriesBoardPoll? SeriesBoardPoll { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }
        public string ChoiceCode { get; set; } = null!;
        public string? VoteReason { get; set; }
        public DateTime VotedAtUtc { get; set; }
    }
}
