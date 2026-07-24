using MangaManagementSystem.Application.Features.Ranking.Warnings;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Infrastructure.Options;
using MangaManagementSystem.Infrastructure.Persistence;
using MangaManagementSystem.Infrastructure.Repositories;
using MangaManagementSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using EFCore.NamingConventions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MangaManagementSystem.Application.Features.EditorialBoard.Repositories;
using MangaManagementSystem.Application.Features.Ranking.Repositories;

namespace MangaManagementSystem.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.TryAddSingleton(TimeProvider.System);
            services.AddScoped<MangaManagementSystem.Infrastructure.Persistence.Interceptors.AuditableEntityInterceptor>();

            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                var interceptor = sp.GetRequiredService<MangaManagementSystem.Infrastructure.Persistence.Interceptors.AuditableEntityInterceptor>();
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                       .AddInterceptors(interceptor)
                       .UseSnakeCaseNamingConvention();
            });

            services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
            // Cloudinary settings and client
            services.Configure<Options.CloudinarySettings>(configuration.GetSection(Options.CloudinarySettings.SectionName));
            var cloudOpts = configuration.GetSection(Options.CloudinarySettings.SectionName).Get<Options.CloudinarySettings>();
            if (cloudOpts != null)
            {
                var account = new CloudinaryDotNet.Account(cloudOpts.CloudName, cloudOpts.ApiKey, cloudOpts.ApiSecret);
                var cloudinary = new CloudinaryDotNet.Cloudinary(account) { Api = { Secure = true } };
                services.AddSingleton(cloudinary);
            }

            // PasswordResetTokenService still uses IMemoryCache.
            services.AddMemoryCache();

            // Local/dev provider for the IDistributedCache abstraction used by OTP.
            // Production can replace this with Redis or SQL distributed cache.
            services.AddDistributedMemoryCache();

            services.AddSingleton<IOtpCacheService, OtpCacheService>();

            services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
            services.AddScoped<IEmailService, EmailService>();

            // Generic repository
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

            // Specific repositories
            services.AddScoped<ISeriesRepository, SeriesRepository>();
            services.AddScoped<IChapterRepository, ChapterRepository>();
            services.AddScoped<IMangakaChapterRepository, MangakaChapterRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IChapterPageTaskRepository, ChapterPageTaskRepository>();
            services.AddScoped<IChapterPageAnnotationRepository, ChapterPageAnnotationRepository>();
            services.AddScoped<IWorkspaceResourceAuthorizationService, WorkspaceResourceAuthorizationService>();
            services.AddScoped<ISeriesProposalRepository, SeriesProposalRepository>();
            services.AddScoped<IEditorDashboardRepository, EditorDashboardRepository>();
            services.AddScoped<IAssistantCompletedWorkRepository, AssistantCompletedWorkRepository>();
            services.AddScoped<IEditorChapterReviewRepository, EditorChapterReviewRepository>();
            services.AddScoped<IEditorAnnotationRepository, EditorAnnotationRepository>();
            services.AddScoped<IEditorSeriesRepository, EditorSeriesRepository>();
            services.AddScoped<IReferenceDataRepository, ReferenceDataRepository>();
            services.AddScoped<ISeriesContributorManagementRepository, SeriesContributorRepository>();
            services.AddScoped<IQuickSelectRepository, QuickSelectRepository>();
            // Unit of Work
            services.AddScoped<IFileResourceRepository, FileResourceRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IPasswordResetTokenService, PasswordResetTokenService>();
            services.AddScoped<IAuditEventRepository, AuditEventRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // File storage (application interface implemented in Infrastructure)
            services.AddScoped<MangaManagementSystem.Application.Interfaces.IFileStorageService, Services.CloudinaryFileStorageService>();
            services.AddScoped<Services.CloudinaryFileStorageFormAdapter>();

            // Assistant task submission
            services.AddScoped<MangaManagementSystem.Application.Interfaces.IAssistantTaskSubmissionService, Services.AssistantTaskSubmissionService>();

            // AI Service. The default HttpClient timeout is 100s, which is far too long to hold a
            // Blazor Server circuit (and the page image it buffered) waiting on a stuck service. 60s is
            // still generous for a real YOLO + manga-OCR pass, including the slow first call that loads
            // the ~400MB OCR model.
            services.AddHttpClient<IAiService, AiService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(60);
            });
            services.AddScoped<IImageMetadataProvider, CloudinaryImageMetadataProvider>();

            services.AddScoped<IEditorialBoardRepository, EditorialBoardRepository>();
            services.AddScoped<ISeriesRankingRepository, SeriesRankingRepository>();
            services.AddScoped<IRankingWarningRepository, RankingWarningRepository>();
            services.AddScoped<IPublicationPeriodRepository, PublicationPeriodRepository>();
            services.AddScoped<IPublicationScheduleRepository, PublicationScheduleRepository>();
            services.AddScoped<IChapterOnHoldRepository, ChapterOnHoldRepository>();
            services.AddScoped<IChapterReleaseRepository, ChapterReleaseRepository>();

            return services;
        }
    }
}
