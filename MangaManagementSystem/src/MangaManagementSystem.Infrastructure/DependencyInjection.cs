using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Interfaces;
using MangaManagementSystem.Infrastructure.Options;
using MangaManagementSystem.Infrastructure.Persistence;
using MangaManagementSystem.Infrastructure.Repositories;
using MangaManagementSystem.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MangaManagementSystem.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));
            services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
            services.AddScoped<IEmailService, EmailService>();

            // Generic repository
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

            // Specific repositories
            services.AddScoped<ISeriesRepository, SeriesRepository>();
            services.AddScoped<IChapterRepository, ChapterRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            // Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}