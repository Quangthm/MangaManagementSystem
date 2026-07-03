using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MangaManagementSystem.Infrastructure.Persistence.Configurations
{
    public class PublicationPeriodConfiguration : IEntityTypeConfiguration<PublicationPeriod>
    {
        public void Configure(EntityTypeBuilder<PublicationPeriod> builder)
        {
            builder.ToTable("PublicationPeriod", "manga");
            builder.HasKey(p => p.PublicationPeriodId);
            builder.Property(p => p.PublicationPeriodId).ValueGeneratedOnAdd();
            builder.Property(p => p.PeriodName).IsRequired().HasMaxLength(100);
            builder.Property(p => p.PeriodTypeCode).IsRequired().HasMaxLength(50);
            builder.Property(p => p.PeriodStartDate).HasColumnType("date").IsRequired();
            builder.Property(p => p.PeriodEndDate).HasColumnType("date").IsRequired();
        }
    }
}
