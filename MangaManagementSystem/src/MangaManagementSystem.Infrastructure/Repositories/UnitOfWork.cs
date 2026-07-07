using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _transaction;
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
        public IAuditEventRepository AuditEvents { get; }
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
            IAuditEventRepository auditEvents,
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

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction != null) return;
            _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction == null) return;
            try
            {
                await _transaction.CommitAsync(cancellationToken);
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_transaction == null) return;
            try
            {
                await _transaction.RollbackAsync(cancellationToken);
                _context.ChangeTracker.Clear();
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
