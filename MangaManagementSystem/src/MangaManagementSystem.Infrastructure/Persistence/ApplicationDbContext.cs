using MangaManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace MangaManagementSystem.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Role> Roles => Set<Role>();
        public DbSet<User> Users => Set<User>();
        public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
        public DbSet<FileResource> FileResources => Set<FileResource>();
        public DbSet<Series> Series => Set<Series>();
        public DbSet<SeriesContributor> SeriesContributors => Set<SeriesContributor>();
        public DbSet<SeriesProposal> SeriesProposals => Set<SeriesProposal>();
        public DbSet<SeriesBoardPoll> SeriesBoardPolls => Set<SeriesBoardPoll>();
        public DbSet<SeriesBoardVote> SeriesBoardVotes => Set<SeriesBoardVote>();
        public DbSet<SeriesBoardPollVoteSummary> SeriesBoardPollVoteSummaries => Set<SeriesBoardPollVoteSummary>();
        public DbSet<Chapter> Chapters => Set<Chapter>();
        public DbSet<ChapterPage> ChapterPages => Set<ChapterPage>();
        public DbSet<ChapterPageVersion> ChapterPageVersions => Set<ChapterPageVersion>();
        public DbSet<PageRegion> PageRegions => Set<PageRegion>();
        public DbSet<ChapterPageAnnotation> ChapterPageAnnotations => Set<ChapterPageAnnotation>();
        public DbSet<ChapterPageTask> ChapterPageTasks => Set<ChapterPageTask>();
        public DbSet<ChapterPageTaskRegion> ChapterPageTaskRegions => Set<ChapterPageTaskRegion>();
        public DbSet<ChapterEditorialReview> ChapterEditorialReviews => Set<ChapterEditorialReview>();
        public DbSet<SeriesRankingSnapshot> SeriesRankingSnapshots => Set<SeriesRankingSnapshot>();
        public DbSet<ChapterReaderVoteSnapshot> ChapterReaderVoteSnapshots => Set<ChapterReaderVoteSnapshot>();
        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            builder.Entity<SeriesBoardPollVoteSummary>()
                .ToView("vw_SeriesBoardPollVoteSummary", "manga")
                .HasNoKey();
        }
    }

    public interface IApplicationDbContext
    {
        DbSet<Role> Roles { get; }
        DbSet<User> Users { get; }
        DbSet<AuditEvent> AuditEvents { get; }
        DbSet<FileResource> FileResources { get; }
        DbSet<Series> Series { get; }
        DbSet<SeriesContributor> SeriesContributors { get; }
        DbSet<SeriesProposal> SeriesProposals { get; }
        DbSet<SeriesBoardPoll> SeriesBoardPolls { get; }
        DbSet<SeriesBoardVote> SeriesBoardVotes { get; }
        DbSet<SeriesBoardPollVoteSummary> SeriesBoardPollVoteSummaries { get; }
        DbSet<Chapter> Chapters { get; }
        DbSet<ChapterPage> ChapterPages { get; }
        DbSet<ChapterPageVersion> ChapterPageVersions { get; }
        DbSet<PageRegion> PageRegions { get; }
        DbSet<ChapterPageAnnotation> ChapterPageAnnotations { get; }
        DbSet<ChapterPageTask> ChapterPageTasks { get; }
        DbSet<ChapterPageTaskRegion> ChapterPageTaskRegions { get; }
        DbSet<ChapterEditorialReview> ChapterEditorialReviews { get; }
        DbSet<SeriesRankingSnapshot> SeriesRankingSnapshots { get; }
        DbSet<ChapterReaderVoteSnapshot> ChapterReaderVoteSnapshots { get; }
        DbSet<Notification> Notifications { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
