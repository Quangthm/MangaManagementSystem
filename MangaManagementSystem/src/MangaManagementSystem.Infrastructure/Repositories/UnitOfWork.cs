using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        public ISeriesRepository Series { get; }
        public IChapterRepository Chapters { get; }
        public IUserRepository Users { get; }
        public IGenericRepository<ChapterPage> ChapterPages { get; }
        public IGenericRepository<FileResource> FileResources { get; }
        public ISeriesProposalRepository SeriesProposals { get; }
        public IGenericRepository<ChapterPageVersion> ChapterPageVersions { get; }
        public IGenericRepository<PageRegion> PageRegions { get; }
        public IChapterPageTaskRepository ChapterPageTasks { get; }
        public IChapterPageAnnotationRepository ChapterPageAnnotations { get; }
        public IGenericRepository<ChapterEditorialReview> ChapterEditorialReviews { get; }
        public IGenericRepository<Role> Roles { get; }
        public IGenericRepository<SeriesContributor> SeriesContributors { get; }
        public IGenericRepository<SeriesBoardPoll> SeriesBoardPolls { get; }
        public IGenericRepository<ChapterReaderVoteSnapshot> ChapterReaderVoteSnapshots { get; }
        public IGenericRepository<SeriesRankingSnapshot> SeriesRankingSnapshots { get; }
        public IGenericRepository<Notification> Notifications { get; }
        public IGenericRepository<AuditEvent> AuditEvents { get; }
        public IGenericRepository<SeriesBoardVote> SeriesBoardVotes { get; }

        public UnitOfWork(
            ApplicationDbContext context,
            ISeriesRepository seriesRepository,
            IChapterRepository chapterRepository,
            IUserRepository userRepository,
            IGenericRepository<ChapterPage> chapterPages,
            IGenericRepository<FileResource> fileResources,
            ISeriesProposalRepository seriesProposals,
            IGenericRepository<ChapterPageVersion> chapterPageVersions,
            IGenericRepository<PageRegion> pageRegions,
            IChapterPageTaskRepository chapterPageTasks,
            IChapterPageAnnotationRepository chapterPageAnnotations,
            IGenericRepository<ChapterEditorialReview> chapterEditorialReviews,
            IGenericRepository<Role> roles,
            IGenericRepository<SeriesContributor> seriesContributors,
            IGenericRepository<SeriesBoardPoll> seriesBoardPolls,
            IGenericRepository<ChapterReaderVoteSnapshot> chapterReaderVoteSnapshots,
            IGenericRepository<SeriesRankingSnapshot> seriesRankingSnapshots,
            IGenericRepository<Notification> notifications,
            IGenericRepository<AuditEvent> auditEvents,
            IGenericRepository<SeriesBoardVote> seriesBoardVotes)
        {
            _context = context;
            Series = seriesRepository;
            Chapters = chapterRepository;
            Users = userRepository;
            ChapterPages = chapterPages;
            FileResources = fileResources;
            SeriesProposals = seriesProposals;
            ChapterPageVersions = chapterPageVersions;
            PageRegions = pageRegions;
            ChapterPageTasks = chapterPageTasks;
            ChapterPageAnnotations = chapterPageAnnotations;
            ChapterEditorialReviews = chapterEditorialReviews;
            Roles = roles;
            SeriesContributors = seriesContributors;
            SeriesBoardPolls = seriesBoardPolls;
            ChapterReaderVoteSnapshots = chapterReaderVoteSnapshots;
            SeriesRankingSnapshots = seriesRankingSnapshots;
            Notifications = notifications;
            AuditEvents = auditEvents;
            SeriesBoardVotes = seriesBoardVotes;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                _context.ChangeTracker.Clear();
                throw;
            }
        }

        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
