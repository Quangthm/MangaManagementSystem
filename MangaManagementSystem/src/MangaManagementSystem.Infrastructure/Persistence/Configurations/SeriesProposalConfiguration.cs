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
            builder.Property(sp => sp.ProposalVersionNo).IsRequired();
            builder.Property(sp => sp.StatusCode).HasMaxLength(50).HasDefaultValue("UNDER_EDITORIAL_REVIEW");
            builder.HasIndex(sp => new { sp.SeriesId, sp.ProposalVersionNo }).IsUnique();
            builder.HasIndex(sp => new { sp.SeriesId, sp.ProposalVersionNo }).HasDatabaseName("ix_series_proposal_series_version");
            builder.HasIndex(sp => sp.StatusCode).HasDatabaseName("ix_series_proposal_status_submitted");
            builder.HasIndex(sp => sp.SubmittedByUserId).HasDatabaseName("ix_series_proposal_submitted_by");
            builder.HasIndex(sp => sp.ReviewedByUserId).HasDatabaseName("ix_series_proposal_reviewed_by").HasFilter("[ReviewedByUserId] IS NOT NULL");
            builder.HasOne(sp => sp.Series).WithMany().HasForeignKey(sp => sp.SeriesId);
            builder.HasOne(sp => sp.ProposalFile).WithMany().HasForeignKey(sp => sp.ProposalFileId);
            builder.HasOne(sp => sp.SubmittedByUser).WithMany().HasForeignKey(sp => sp.SubmittedByUserId);
            builder.HasOne(sp => sp.ReviewedByUser).WithMany().HasForeignKey(sp => sp.ReviewedByUserId);
            builder.HasOne(sp => sp.MarkupFile).WithMany().HasForeignKey(sp => sp.MarkupFileId);
        }
    }
}
