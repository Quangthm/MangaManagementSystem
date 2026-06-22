using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaManagementSystem.Infrastructure.Persistence.Configurations
{
    public class SeriesProposalConfiguration : IEntityTypeConfiguration<SeriesProposal>
    {
        public void Configure(EntityTypeBuilder<SeriesProposal> builder)
        {
            builder.ToTable("SeriesProposal", "manga");
            builder.HasKey(sp => sp.SeriesProposalId);
            builder.Property(sp => sp.SeriesProposalId).ValueGeneratedOnAdd();
            builder.Ignore(sp => sp.GenreSnapshot); // Genre snapshot column removed from DB; to be replaced by Series.Genres join in Phase 2
            builder.Property(sp => sp.ProposalVersionNo).IsRequired();
            builder.Property(sp => sp.ProposalTitle).IsRequired().HasMaxLength(200);
            builder.Property(sp => sp.SynopsisSnapshot).IsRequired();
            builder.Property(sp => sp.StatusCode).HasMaxLength(50).HasDefaultValue("UNDER_EDITORIAL_REVIEW");
            builder.Property(sp => sp.SubmittedAtUtc).IsRequired();
            builder.Property(sp => sp.Comments);
            builder.Property(sp => sp.ReviewedAtUtc);
            builder.HasIndex(sp => new { sp.SeriesId, sp.ProposalVersionNo }).IsUnique();
            builder.HasOne(sp => sp.Series).WithMany().HasForeignKey(sp => sp.SeriesId);
            builder.HasOne(sp => sp.ProposalFile).WithMany().HasForeignKey(sp => sp.ProposalFileId);
            builder.HasOne(sp => sp.SubmittedByUser).WithMany().HasForeignKey(sp => sp.SubmittedByUserId);
            builder.HasOne(sp => sp.ReviewedByUser).WithMany().HasForeignKey(sp => sp.ReviewedByUserId);
            builder.HasOne(sp => sp.MarkupFile).WithMany().HasForeignKey(sp => sp.MarkupFileId);
        }
    }
}
