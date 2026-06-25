using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MangaManagementSystem.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // MediatR — registers all IRequestHandler<,> implementations in this assembly.
            // New CQRS workflows (e.g. SubmitSeriesProposalCommand) are picked up automatically.
            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));


            services.AddScoped<ISeriesService, SeriesService>();
            services.AddScoped<IChapterService, ChapterService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IChapterPageService, ChapterPageService>();
            services.AddScoped<IFileResourceService, FileResourceService>();
            services.AddScoped<ISeriesProposalService, SeriesProposalService>();
            services.AddScoped<IChapterPageVersionService, ChapterPageVersionService>();
            services.AddScoped<IPageRegionService, PageRegionService>();
            services.AddScoped<IChapterPageTaskService, ChapterPageTaskService>();
            services.AddScoped<IChapterPageAnnotationService, ChapterPageAnnotationService>();
            services.AddScoped<IChapterEditorialReviewService, ChapterEditorialReviewService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<ISeriesContributorService, SeriesContributorService>();
            services.AddScoped<ISeriesBoardPollService, SeriesBoardPollService>();
            services.AddScoped<IChapterReaderVoteSnapshotService, ChapterReaderVoteSnapshotService>();
            services.AddScoped<ISeriesRankingSnapshotService, SeriesRankingSnapshotService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IAuditEventService, AuditEventService>();
            services.AddScoped<ISeriesBoardVoteService, SeriesBoardVoteService>();
            services.AddScoped<IQuickSelectService, QuickSelectService>();
            return services;
        }
    }
}
