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
            builder.Property(v => v.ChoiceCode).IsRequired().HasMaxLength(20);
            builder.Property(v => v.CreatedAtUtc).IsRequired();
            builder.HasIndex(v => new { v.SeriesBoardPollId, v.UserId }).IsUnique();
            builder.HasCheckConstraint("CK_SeriesBoardVote_ChoiceCode", "[ChoiceCode] IN ('APPROVE','REJECT','ABSTAIN')");
            builder.HasOne(v => v.SeriesBoardPoll).WithMany().HasForeignKey(v => v.SeriesBoardPollId);
            builder.HasOne(v => v.User).WithMany().HasForeignKey(v => v.UserId);
        }
    }
}
