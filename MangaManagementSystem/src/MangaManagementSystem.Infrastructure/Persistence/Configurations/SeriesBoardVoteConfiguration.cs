using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaManagementSystem.Infrastructure.Persistence.Configurations
{
    public class SeriesBoardVoteConfiguration : IEntityTypeConfiguration<SeriesBoardVote>
    {
        public void Configure(EntityTypeBuilder<SeriesBoardVote> builder)
        {
            builder.ToTable("SeriesBoardVote", "manga");
            builder.HasKey(v => v.SeriesBoardVoteId);
            builder.Property(v => v.SeriesBoardVoteId).ValueGeneratedOnAdd();
            builder.Property(v => v.ChoiceCode).IsRequired().HasMaxLength(50);
            builder.Property(v => v.VoteReason).HasMaxLength(500);
            builder.Property(v => v.VotedAtUtc).IsRequired();
            builder.HasIndex(v => new { v.SeriesBoardPollId, v.UserId }).IsUnique();
            builder.HasOne(v => v.SeriesBoardPoll).WithMany(p => p.Votes).HasForeignKey(v => v.SeriesBoardPollId);
            builder.HasOne(v => v.User).WithMany().HasForeignKey(v => v.UserId);
        }
    }
}
