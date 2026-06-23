using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    public sealed class SeriesContributorRepository : ISeriesContributorManagementRepository
    {
        private readonly ApplicationDbContext _context;

        public SeriesContributorRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IsActiveMangakaContributorAsync(
            Guid actorUserId,
            Guid seriesId,
            CancellationToken cancellationToken = default)
        {
            return await _context.ActiveSeriesContributors
                .AsNoTracking()
                .AnyAsync(asc => asc.SeriesId == seriesId
                              && asc.UserId == actorUserId
                              && asc.RoleName == "Mangaka"
                              && asc.UserStatusCode == "ACTIVE",
                          cancellationToken);
        }

        public async Task<IReadOnlyList<SeriesContributorListItemDto>> GetSeriesContributorsAsync(
            Guid actorUserId,
            Guid seriesId,
            CancellationToken cancellationToken = default)
        {
            var query =
                from sc in _context.SeriesContributors.AsNoTracking()
                join u in _context.Users.AsNoTracking() on sc.UserId equals u.UserId
                join r in _context.Roles.AsNoTracking() on u.RoleId equals r.RoleId
                join s in _context.Series.AsNoTracking() on sc.SeriesId equals s.SeriesId
                where sc.SeriesId == seriesId
                orderby (sc.EndDate == null && u.StatusCode == "ACTIVE") descending, u.DisplayName
                select new SeriesContributorListItemDto(
                    sc.SeriesId,
                    s.Title,
                    sc.UserId,
                    u.DisplayName,
                    u.Username,
                    u.Email,
                    r.RoleName,
                    sc.StartDate,
                    sc.EndDate,
                    sc.EndDate == null && u.StatusCode == "ACTIVE");

            return await query.ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<EligibleAssistantContributorDto>> SearchEligibleAssistantContributorsAsync(
            Guid actorUserId,
            Guid seriesId,
            string? search,
            CancellationToken cancellationToken = default)
        {
            var activeContributorUserIds = _context.SeriesContributors
                .AsNoTracking()
                .Where(sc => sc.SeriesId == seriesId && sc.EndDate == null)
                .Select(sc => sc.UserId);

            var query =
                from u in _context.Users.AsNoTracking()
                join r in _context.Roles.AsNoTracking() on u.RoleId equals r.RoleId
                where r.RoleName == "Assistant"
                      && u.StatusCode == "ACTIVE"
                      && !activeContributorUserIds.Contains(u.UserId)
                select new
                {
                    u.UserId,
                    u.DisplayName,
                    u.Username,
                    u.Email
                };

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x =>
                    x.DisplayName.Contains(term)
                    || (x.Username != null && x.Username.Contains(term))
                    || (x.Email != null && x.Email.Contains(term)));
            }

            return await query
                .OrderBy(x => x.DisplayName)
                .ThenBy(x => x.Username)
                .Take(50)
                .Select(x => new EligibleAssistantContributorDto(
                    x.UserId,
                    x.DisplayName,
                    x.Username,
                    x.Email))
                .ToListAsync(cancellationToken);
        }

        public async Task<(bool Exists, string? DisplayName, string? Username, string? Email, string? RoleName, string? StatusCode, bool IsActiveContributor)> GetContributorTargetSnapshotAsync(
            Guid seriesId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var userRow = await (
                from u in _context.Users.AsNoTracking()
                join r in _context.Roles.AsNoTracking() on u.RoleId equals r.RoleId
                where u.UserId == userId
                select new
                {
                    u.DisplayName,
                    u.Username,
                    u.Email,
                    RoleName = r.RoleName,
                    u.StatusCode
                }).FirstOrDefaultAsync(cancellationToken);

            if (userRow == null)
            {
                return (Exists: false, null, null, null, null, null, IsActiveContributor: false);
            }

            bool isActiveContributor = await _context.SeriesContributors
                .AsNoTracking()
                .AnyAsync(sc => sc.SeriesId == seriesId
                              && sc.UserId == userId
                              && sc.EndDate == null,
                          cancellationToken);

            return (
                Exists: true,
                DisplayName: userRow.DisplayName,
                Username: userRow.Username,
                Email: userRow.Email,
                RoleName: userRow.RoleName,
                StatusCode: userRow.StatusCode,
                IsActiveContributor: isActiveContributor);
        }

        public async Task<bool> HasActiveTasksForSeriesAsync(
            Guid assistantUserId,
            Guid seriesId,
            CancellationToken cancellationToken = default)
        {
            return await (
                from t in _context.ChapterPageTasks.AsNoTracking()
                from r in t.PageRegions
                join cpv in _context.Set<Domain.Entities.ChapterPageVersion>().AsNoTracking()
                    on r.ChapterPageVersionId equals cpv.ChapterPageVersionId
                join cp in _context.Set<Domain.Entities.ChapterPage>().AsNoTracking()
                    on cpv.ChapterPageId equals cp.ChapterPageId
                join ch in _context.Set<Domain.Entities.Chapter>().AsNoTracking()
                    on cp.ChapterId equals ch.ChapterId
                where ch.SeriesId == seriesId
                      && t.AssignedToUserId == assistantUserId
                      && (t.StatusCode == "ASSIGNED" || t.StatusCode == "UNDER_REVIEW")
                select 1
            ).AnyAsync(cancellationToken);
        }

        public async Task AddContributorViaProcAsync(
            Guid actorUserId,
            Guid seriesId,
            Guid userId,
            string? notes,
            CancellationToken cancellationToken = default)
        {
            var conn = _context.Database.GetDbConnection();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "manga.usp_SeriesContributor_Add";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@actor_user_id", SqlDbType.UniqueIdentifier) { Value = actorUserId });
            cmd.Parameters.Add(new SqlParameter("@series_id", SqlDbType.UniqueIdentifier) { Value = seriesId });
            cmd.Parameters.Add(new SqlParameter("@user_id", SqlDbType.UniqueIdentifier) { Value = userId });
            cmd.Parameters.Add(new SqlParameter("@notes", SqlDbType.NVarChar, 500) { Value = (object?)notes ?? DBNull.Value });

            var outputParam = new SqlParameter("@new_series_contributor_id", SqlDbType.UniqueIdentifier)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(outputParam);

            await ExecuteNonQueryWithConnectionAsync(cmd, cancellationToken);
        }

        public async Task EndAssistantContributorViaProcAsync(
            Guid actorUserId,
            Guid seriesId,
            Guid assistantUserId,
            string reason,
            CancellationToken cancellationToken = default)
        {
            var conn = _context.Database.GetDbConnection();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "manga.usp_SeriesContributor_EndAssistant";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@actor_user_id", SqlDbType.UniqueIdentifier) { Value = actorUserId });
            cmd.Parameters.Add(new SqlParameter("@series_id", SqlDbType.UniqueIdentifier) { Value = seriesId });
            cmd.Parameters.Add(new SqlParameter("@assistant_user_id", SqlDbType.UniqueIdentifier) { Value = assistantUserId });
            cmd.Parameters.Add(new SqlParameter("@reason", SqlDbType.NVarChar, 500) { Value = reason });

            await ExecuteNonQueryWithConnectionAsync(cmd, cancellationToken);
        }

        private static async Task ExecuteNonQueryWithConnectionAsync(DbCommand cmd, CancellationToken cancellationToken)
        {
            if (cmd.Connection!.State != ConnectionState.Open)
            {
                await cmd.Connection.OpenAsync(cancellationToken);
            }

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
