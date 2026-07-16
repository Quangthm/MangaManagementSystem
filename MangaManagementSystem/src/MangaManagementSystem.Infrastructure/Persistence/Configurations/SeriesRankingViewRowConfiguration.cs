using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaManagementSystem.Infrastructure.Persistence.Configurations;

public sealed class SeriesRankingViewRowConfiguration : IEntityTypeConfiguration<SeriesRankingViewRow>
{
    public void Configure(EntityTypeBuilder<SeriesRankingViewRow> builder)
    {
        builder.ToView("vw_SeriesRanking", "manga");
        builder.HasNoKey();

        builder.Property(r => r.AverageRating)
            .HasColumnType("decimal(4,2)");

        builder.Property(r => r.RankingScore)
            .HasColumnType("decimal(18,4)");
    }
}
