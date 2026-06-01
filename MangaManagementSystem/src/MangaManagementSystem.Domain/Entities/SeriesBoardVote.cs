using System;

namespace MangaManagementSystem.Domain.Entities
{
    public class SeriesBoardVote
    {
        public long SeriesBoardVoteId { get; set; }
        public long SeriesBoardPollId { get; set; }
        public SeriesBoardPoll? SeriesBoardPoll { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public string ChoiceCode { get; set; } = null!;
        public string? VoteReason { get; set; }
        public DateTime VotedAtUtc { get; set; }
    }
}
