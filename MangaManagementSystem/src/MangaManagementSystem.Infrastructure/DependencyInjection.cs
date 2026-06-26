using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Infrastructure.Options;
using MangaManagementSystem.Infrastructure.Persistence;
using MangaManagementSystem.Infrastructure.Repositories;
using MangaManagementSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using EFCore.NamingConventions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MangaManagementSystem.Application.Features.EditorialBoard.Repositories;

namespace MangaManagementSystem.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                    .UseSnakeCaseNamingConvention());

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
            services.AddMemoryCache();
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
            services.AddScoped<ISeriesProposalRepository, SeriesProposalRepository>();
            services.AddScoped<IEditorDashboardRepository, EditorDashboardRepository>();
            services.AddScoped<IAssistantCompletedWorkRepository, AssistantCompletedWorkRepository>();
            services.AddScoped<IEditorChapterReviewRepository, EditorChapterReviewRepository>();
            services.AddScoped<IEditorAnnotationRepository, EditorAnnotationRepository>();
            services.AddScoped<IEditorSeriesRepository, EditorSeriesRepository>();
            services.AddScoped<IReferenceDataRepository, ReferenceDataRepository>();
            services.AddScoped<ISeriesContributorManagementRepository, SeriesContributorRepository>();
            services.AddScoped<IQuickSelectRepository, QuickSelectRepository>();
            services.AddScoped<ILandingPageRepository, LandingPageRepository>();

            // Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // File storage (application interface implemented in Infrastructure)
            services.AddScoped<MangaManagementSystem.Application.Interfaces.IFileStorageService, Services.CloudinaryFileStorageService>();
            services.AddScoped<Services.CloudinaryFileStorageFormAdapter>();

            // Assistant task submission
            services.AddScoped<MangaManagementSystem.Application.Interfaces.IAssistantTaskSubmissionService, Services.AssistantTaskSubmissionService>();

            // AI Service
            services.AddHttpClient<IAiService, AiService>();
            services.AddScoped<IImageMetadataProvider, CloudinaryImageMetadataProvider>();

            services.AddScoped<IEditorialBoardRepository, EditorialBoardRepository>();
 
            return services;
        }
    }
}

