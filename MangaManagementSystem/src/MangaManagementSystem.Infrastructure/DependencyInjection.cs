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
using System;

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

            services.Configure<CloudinarySettings>(
                configuration.GetSection(CloudinarySettings.SectionName)
            );

            var cloudOpts = configuration
                .GetSection(CloudinarySettings.SectionName)
                .Get<CloudinarySettings>();

            var hasValidCloudinaryConfig =
                cloudOpts != null &&
                !string.IsNullOrWhiteSpace(cloudOpts.CloudName) &&
                !string.IsNullOrWhiteSpace(cloudOpts.ApiKey) &&
                !string.IsNullOrWhiteSpace(cloudOpts.ApiSecret) &&
                !cloudOpts.CloudName.StartsWith("DAN_", StringComparison.OrdinalIgnoreCase) &&
                !cloudOpts.ApiKey.StartsWith("DAN_", StringComparison.OrdinalIgnoreCase) &&
                !cloudOpts.ApiSecret.StartsWith("DAN_", StringComparison.OrdinalIgnoreCase);

            if (hasValidCloudinaryConfig)
            {
                var account = new CloudinaryDotNet.Account(
                    cloudOpts!.CloudName,
                    cloudOpts.ApiKey,
                    cloudOpts.ApiSecret
                );

                var cloudinary = new CloudinaryDotNet.Cloudinary(account)
                {
                    Api = { Secure = true }
                };

                services.AddSingleton(cloudinary);

                services.AddScoped<IFileStorageService, CloudinaryFileStorageService>();
                services.AddScoped<CloudinaryFileStorageFormAdapter>();
            }
            else
            {
                services.AddScoped<IFileStorageService, NullFileStorageService>();
            }

            services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
            services.AddScoped<IEmailService, EmailService>();

            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

            services.AddScoped<ISeriesRepository, SeriesRepository>();
            services.AddScoped<IChapterRepository, ChapterRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}