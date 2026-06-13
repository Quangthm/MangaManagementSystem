using MangaManagementSystem.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        ISeriesRepository Series { get; }
        IChapterRepository Chapters { get; }
        IUserRepository Users { get; }
        IGenericRepository<ChapterPage> ChapterPages { get; }
        IGenericRepository<FileResource> FileResources { get; }
        ISeriesProposalRepository SeriesProposals { get; }
        IGenericRepository<ChapterPageVersion> ChapterPageVersions { get; }
        IGenericRepository<PageRegion> PageRegions { get; }
        IChapterPageTaskRepository ChapterPageTasks { get; }
        IChapterPageAnnotationRepository ChapterPageAnnotations { get; }
        IGenericRepository<ChapterEditorialReview> ChapterEditorialReviews { get; }
        IGenericRepository<Role> Roles { get; }
        IGenericRepository<SeriesContributor> SeriesContributors { get; }
        IGenericRepository<SeriesBoardPoll> SeriesBoardPolls { get; }
        IGenericRepository<ChapterReaderVoteSnapshot> ChapterReaderVoteSnapshots { get; }
        IGenericRepository<SeriesRankingSnapshot> SeriesRankingSnapshots { get; }
        IGenericRepository<Notification> Notifications { get; }
        IGenericRepository<AuditEvent> AuditEvents { get; }
        IGenericRepository<SeriesBoardVote> SeriesBoardVotes { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
