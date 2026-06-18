using MangaManagementSystem.Infrastructure;
using MangaManagementSystem.Infrastructure.Persistence;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Common;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Cryptography;
using System.Text;

namespace CreateTestSeries
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer("Server=localhost;Database=MangaManagementDB;User Id=sa;Password=12345;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=true")
                .Options;

            using var context = new ApplicationDbContext(options);

            var assistantRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Assistant");
            if (assistantRole == null)
            {
                Console.WriteLine("Creating Assistant role...");
                assistantRole = new Role
                {
                    RoleId = Guid.NewGuid(),
                    RoleName = "Assistant",
                };
                context.Roles.Add(assistantRole);
                await context.SaveChangesAsync();
            }

            // Get or create the mangaka role
            var mangakaRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Mangaka");
            if (mangakaRole == null)
            {
                Console.WriteLine("Creating Mangaka role...");
                mangakaRole = new Role
                {
                    RoleId = Guid.NewGuid(),
                    RoleName = "Mangaka",
                };
                context.Roles.Add(mangakaRole);
                await context.SaveChangesAsync();
            }

            // Get or create user "troly" (Assistant)
            var trolyUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "troly");
            if (trolyUser == null)
            {
                Console.WriteLine("Creating user 'troly'...");
                var passwordHash = HashPassword("Password123!");
                trolyUser = new User
                {
                    UserId = Guid.NewGuid(),
                    RoleId = assistantRole!.RoleId,
                    Username = "troly",
                    Email = "troly@example.com",
                    DisplayName = "Troly Assistant",
                    PasswordHash = passwordHash,
                    StatusCode = "ACTIVE",
                    CreatedAtUtc = DateTime.UtcNow
                };
                context.Users.Add(trolyUser);
                await context.SaveChangesAsync();
            }

            // Get or create user "khoavq" (Mangaka)
            var mangakaUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "khoavq");
            if (mangakaUser == null)
            {
                Console.WriteLine("Creating user 'khoavq'...");
                var passwordHash = HashPassword("Password123!");
                mangakaUser = new User
                {
                    UserId = Guid.NewGuid(),
                    RoleId = mangakaRole!.RoleId,
                    Username = "khoavq",
                    Email = "khoavq@example.com",
                    DisplayName = "Khoa VQ Mangaka",
                    PasswordHash = passwordHash,
                    StatusCode = "ACTIVE",
                    CreatedAtUtc = DateTime.UtcNow
                };
                context.Users.Add(mangakaUser);
                await context.SaveChangesAsync();
            }

            // Create approved series
            var series = new Series
            {
                SeriesId = Guid.NewGuid(),
                Title = "Test Series for Troly",
                Slug = "test-series-troly",
                Synopsis = "This is a test series created for verifying assistant task functionality.",
                Genre = "Action, Adventure",
                StatusCode = "APPROVED",
                ContentLanguageCode = "vi",
                CreatedAtUtc = DateTime.UtcNow
            };
            context.Series.Add(series);
            await context.SaveChangesAsync();
            Console.WriteLine($"Created series: {series.SeriesId}");

            // Create series contributor for mangaka
            var seriesContributor = new SeriesContributor
            {
                SeriesContributorId = Guid.NewGuid(),
                SeriesId = series.SeriesId,
                UserId = mangakaUser!.UserId,
                StartDate = DateTime.UtcNow
            };
            context.SeriesContributors.Add(seriesContributor);
            await context.SaveChangesAsync();
            Console.WriteLine("Added series contributor");

            // Create chapter
            var chapter = new Chapter
            {
                ChapterId = Guid.NewGuid(),
                SeriesId = series.SeriesId,
                ChapterNumberLabel = "01",
                ChapterTitle = "Chapter 1: The Beginning",
                StatusCode = "APPROVED",
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = mangakaUser!.UserId
            };
            context.Chapters.Add(chapter);
            await context.SaveChangesAsync();
            Console.WriteLine($"Created chapter: {chapter.ChapterId}");

            // Create 5 pages for the chapter
            var chapterPages = new List<ChapterPage>();
            for (int i = 1; i <= 5; i++)
            {
                var page = new ChapterPage
                {
                    ChapterPageId = Guid.NewGuid(),
                    ChapterId = chapter.ChapterId,
                    PageNo = i,
                    PageNotes = $"Page {i} notes"
                };
                chapterPages.Add(page);
            }
            context.ChapterPages.AddRange(chapterPages.ToArray());
            await context.SaveChangesAsync();
            Console.WriteLine($"Created {chapterPages.Count} pages");

            // Create tasks for each page assigned to troly
            var tasks = new List<ChapterPageTask>();
            for (int i = 0; i < chapterPages.Count; i++)
            {
                var task = new ChapterPageTask
                {
                    ChapterPageTaskId = Guid.NewGuid(),
                    AssignedToUserId = trolyUser!.UserId,
                    TypeCode = "COLORING",
                    StatusCode = "ASSIGNED",
                    TaskTitle = $"Coloring Task for Page {i + 1}",
                    TaskDescription = $"Please color page {i + 1} of the series. Focus on the background elements.",
                    PriorityLevel = 3,
                    DueAtUtc = DateTime.UtcNow.AddDays(7),
                    CompensationAmount = 50.00m,
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedByUserId = mangakaUser!.UserId
                };
                tasks.Add(task);
            }
            context.ChapterPageTasks.AddRange(tasks.ToArray());
            await context.SaveChangesAsync();
            Console.WriteLine($"Created {tasks.Count} tasks for user 'troly'");

            // Create notifications for troly
            var notifications = new List<Notification>();
            foreach (var task in tasks)
            {
                var notification = new Notification
                {
                    NotificationId = Guid.NewGuid(),
                    RecipientUserId = trolyUser!.UserId,
                    NotificationTypeCode = "NEW_TASK_ASSIGNED",
                    Title = "New Task Assigned",
                    Message = $"You have been assigned a new coloring task: {task.TaskTitle}",
                    RelatedEntityType = "ChapterPageTask",
                    RelatedEntityId = task.ChapterPageTaskId,
                    CreatedAtUtc = DateTime.UtcNow
                };
                notifications.Add(notification);
            }
            context.Notifications.AddRange(notifications.ToArray());
            await context.SaveChangesAsync();
            Console.WriteLine($"Created {notifications.Count} notifications");

            Console.WriteLine("\n=== Test Data Created Successfully ===");
            Console.WriteLine($"Series ID: {series.SeriesId}");
            Console.WriteLine($"Series Title: {series.Title}");
            Console.WriteLine($"Chapter ID: {chapter.ChapterId}");
            Console.WriteLine($"Chapter Number: {chapter.ChapterNumberLabel}");
            Console.WriteLine($"Pages: {chapterPages.Count}");
            Console.WriteLine($"Tasks: {tasks.Count}");
            Console.WriteLine($"Notifications: {notifications.Count}");
            Console.WriteLine("\nUser Credentials:");
            Console.WriteLine($"Username: troly | Password: Password123!");
            Console.WriteLine($"Username: khoavq | Password: Password123!");
        }

        static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }
}
