using MangaManagementSystem.Domain.Common;
using System;

namespace MangaManagementSystem.Domain.Entities
{
    public class SeriesBoardVote : BaseEntity
    {
        public long SeriesBoardVoteId { get; set; }
        public long SeriesBoardPollId { get; set; }
        public SeriesBoardPoll? SeriesBoardPoll { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public string ChoiceCode { get; set; } = null!;
        public string? Reason { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
