using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.Infrastructure.Repositories
{
    public class QuickSelectRepository : IQuickSelectRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<QuickSelectRepository> _logger;

        public QuickSelectRepository(
            ApplicationDbContext context,
            ILogger<QuickSelectRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IReadOnlyList<QuickSelectChapterDto>> GetQuickSelectChaptersAsync(
            Guid seriesId, CancellationToken cancellationToken = default)
        {
            var chapters = await _context.Chapters
                .AsNoTracking()
                .Where(c => c.SeriesId == seriesId)
                .OrderBy(c => c.ChapterNumberLabel)
                .Select(c => new
                {
                    c.ChapterId,
                    c.ChapterNumberLabel,
                    c.ChapterTitle,
                    c.StatusCode,
                    PageCount = _context.ChapterPages
                        .Count(cp => cp.ChapterId == c.ChapterId && cp.DeletedAtUtc == null)
                })
                .ToListAsync(cancellationToken);

            return chapters.Select(c => new QuickSelectChapterDto(
                c.ChapterId,
                int.TryParse(c.ChapterNumberLabel, out var num) ? num : 0,
                c.ChapterTitle,
                c.StatusCode,
                c.PageCount
            )).ToList();
        }

        public async Task<IReadOnlyList<QuickSelectPageDto>> GetQuickSelectPagesAsync(
            Guid chapterId, CancellationToken cancellationToken = default)
        {
            var pages = await _context.ChapterPages
                .AsNoTracking()
                .Where(cp => cp.ChapterId == chapterId && cp.DeletedAtUtc == null)
                .Join(
                    _context.ChapterPageVersions.Where(v => v.IsCurrentVersion),
                    cp => cp.ChapterPageId,
                    v => v.ChapterPageId,
                    (cp, v) => new { cp.ChapterPageId, cp.PageNo, Version = v })
                .OrderBy(x => x.PageNo)
                .Select(x => new QuickSelectPageDto(
                    x.ChapterPageId,
                    x.Version.ChapterPageVersionId,
                    x.PageNo,
                    x.Version.VersionNo
                ))
                .ToListAsync(cancellationToken);

            return pages;
        }

        public async Task<IReadOnlyList<QuickSelectAssistantDto>> GetQuickSelectAssistantsAsync(
            Guid seriesId, CancellationToken cancellationToken = default)
        {
            var assistants = await _context.SeriesContributors
                .AsNoTracking()
                .Where(sc => sc.SeriesId == seriesId && sc.EndDate == null)
                .Join(
                    _context.Users.Where(u => u.StatusCode == "ACTIVE"),
                    sc => sc.UserId,
                    u => u.UserId,
                    (sc, u) => new { sc, u }
                )
                .Join(
                    _context.Roles.Where(r => r.RoleName == "Assistant"),
                    x => x.u.RoleId,
                    r => r.RoleId,
                    (x, r) => new QuickSelectAssistantDto(
                        x.u.UserId,
                        x.u.DisplayName,
                        x.u.Username,
                        x.u.Email
                    )
                )
                .OrderBy(x => x.DisplayName)
                .ToListAsync(cancellationToken);

            return assistants;
        }

        public async Task<QuickSelectTaskAssignmentResult> PersistQuickSelectAssignmentAsync(
            QuickSelectAssignmentPlan plan, CancellationToken cancellationToken = default)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    await AcquireAppLockAsync(plan, cancellationToken);

                    await RecheckGuardsAsync(plan, cancellationToken);

                    var now = DateTime.UtcNow;
                    var regionLookup = await ResolveFullPageRegionsAsync(plan, now, cancellationToken);

                    var tasks = new List<ChapterPageTask>();
                    var auditEvents = new List<AuditEvent>();
                    var createdDtos = new List<QuickSelectCreatedTaskDto>();

                    foreach (var item in plan.Items)
                    {
                        var fullPageRegion = regionLookup[item.ChapterPageVersionId];

                        var task = new ChapterPageTask
                        {
                            ChapterPageTaskId = item.ChapterPageTaskId,
                            AssignedToUserId = plan.AssignedToUserId,
                            TypeCode = plan.TypeCode,
                            StatusCode = "ASSIGNED",
                            TaskTitle = item.FinalTaskTitle,
                            TaskDescription = item.FinalTaskDescription,
                            PriorityLevel = plan.PriorityLevel,
                            DueAtUtc = plan.DueAtUtc,
                            CompensationAmount = plan.CompensationAmount,
                            CompletedPageVersionId = null,
                            CreatedAtUtc = now,
                            CreatedByUserId = plan.ActorUserId,
                            UpdatedAtUtc = null,
                            PageRegions = new List<PageRegion> { fullPageRegion }
                        };

                        tasks.Add(task);

                        createdDtos.Add(new QuickSelectCreatedTaskDto(
                            ChapterPageTaskId: item.ChapterPageTaskId,
                            ChapterPageId: item.ChapterPageId,
                            ChapterPageVersionId: item.ChapterPageVersionId,
                            PageNo: item.PageNo
                        ));

                        var auditDetail = BuildAuditDetailJson(item, plan, fullPageRegion.PageRegionId);
                        auditEvents.Add(new AuditEvent
                        {
                            OccurredAtUtc = now,
                            ActorUserId = plan.ActorUserId,
                            ActorRoleName = null,
                            ActionCode = "CHAPTER_PAGE_TASK_CREATED",
                            EntityType = "ChapterPageTask",
                            EntityId = item.ChapterPageTaskId.ToString(),
                            DetailJson = auditDetail
                        });
                    }

                    _context.ChapterPageTasks.AddRange(tasks);
                    _context.AuditEvents.AddRange(auditEvents);

                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    return new QuickSelectTaskAssignmentResult(
                        CreatedTaskCount: createdDtos.Count,
                        CreatedTasks: createdDtos
                    );
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }

        private async Task AcquireAppLockAsync(
            QuickSelectAssignmentPlan plan, CancellationToken ct)
        {
            var lockResource = $"manga_quick_select_task_assignment_{plan.ActorUserId}_{plan.SeriesId}_{plan.ChapterId}";

            var lockResultParam = new SqlParameter("@result", System.Data.SqlDbType.Int)
            {
                Direction = System.Data.ParameterDirection.Output
            };

            await _context.Database.ExecuteSqlRawAsync(
                "EXEC sp_getapplock @Resource, @LockMode, @LockOwner, @LockTimeout, @Result OUTPUT",
                new SqlParameter("@Resource", lockResource),
                new SqlParameter("@LockMode", "Exclusive"),
                new SqlParameter("@LockOwner", "Transaction"),
                new SqlParameter("@LockTimeout", 5000),
                lockResultParam);

            var lockResult = (int)lockResultParam.Value;
            if (lockResult < 0)
            {
                throw new InvalidOperationException(
                    "Another Quick Select assignment is already in progress for this chapter. Please wait and try again.");
            }
        }

        private async Task RecheckGuardsAsync(
            QuickSelectAssignmentPlan plan, CancellationToken ct)
        {
            // Actor is still active Mangaka contributor
            var actorActive = await _context.SeriesContributors
                .AnyAsync(sc =>
                    sc.SeriesId == plan.SeriesId &&
                    sc.UserId == plan.ActorUserId &&
                    sc.EndDate == null, ct);

            if (!actorActive)
                throw new InvalidOperationException(
                    "You no longer have permission to assign tasks for this series.");

            // Assigned user is still ACTIVE Assistant contributor
            var assistantActive = await _context.SeriesContributors
                .AnyAsync(sc =>
                    sc.SeriesId == plan.SeriesId &&
                    sc.UserId == plan.AssignedToUserId &&
                    sc.EndDate == null, ct);

            if (!assistantActive)
                throw new InvalidOperationException(
                    "Assistant is no longer an active contributor for this series.");

            // Chapter still belongs to series
            var chapterValid = await _context.Chapters
                .AnyAsync(c => c.ChapterId == plan.ChapterId && c.SeriesId == plan.SeriesId, ct);

            if (!chapterValid)
                throw new InvalidOperationException(
                    "Selected chapter no longer belongs to this series.");

            foreach (var item in plan.Items)
            {
                // Page belongs to chapter
                var page = await _context.ChapterPages
                    .FirstOrDefaultAsync(cp =>
                        cp.ChapterPageId == item.ChapterPageId &&
                        cp.ChapterId == plan.ChapterId &&
                        cp.DeletedAtUtc == null, ct);

                if (page == null)
                    throw new InvalidOperationException(
                        "Selected page no longer belongs to this chapter.");

                // Version belongs to page
                var version = await _context.ChapterPageVersions
                    .FirstOrDefaultAsync(cpv =>
                        cpv.ChapterPageVersionId == item.ChapterPageVersionId &&
                        cpv.ChapterPageId == item.ChapterPageId, ct);

                if (version == null)
                    throw new InvalidOperationException(
                        "Selected page version no longer belongs to this page.");

                // Version is still current
                if (!version.IsCurrentVersion)
                    throw new InvalidOperationException(
                        "Selected page version is no longer current.");

                // Version still points to same page file
                if (version.PageFileId != item.PageFileResourceId)
                    throw new InvalidOperationException(
                        "Selected page file changed. Please reload Quick Select and try again.");
            }
        }

        private async Task<Dictionary<Guid, PageRegion>> ResolveFullPageRegionsAsync(
            QuickSelectAssignmentPlan plan, DateTime now, CancellationToken ct)
        {
            var result = new Dictionary<Guid, PageRegion>();
            var versionIds = plan.Items.Select(i => i.ChapterPageVersionId).Distinct().ToList();

            var existingRegions = await _context.PageRegions
                .Where(pr =>
                    versionIds.Contains(pr.ChapterPageVersionId) &&
                    pr.TypeCode == "FULL_PAGE")
                .ToListAsync(ct);

            var existingMap = existingRegions
                .GroupBy(r => r.ChapterPageVersionId)
                .ToDictionary(g => g.Key, g => g.First());

            var newRegions = new List<PageRegion>();

            foreach (var item in plan.Items)
            {
                if (existingMap.TryGetValue(item.ChapterPageVersionId, out var existing))
                {
                    result[item.ChapterPageVersionId] = existing;
                }
                else
                {
                    var newRegion = new PageRegion
                    {
                        PageRegionId = Guid.NewGuid(),
                        ChapterPageVersionId = item.ChapterPageVersionId,
                        TypeCode = "FULL_PAGE",
                        RegionLabel = "Full page",
                        X = 0,
                        Y = 0,
                        Width = item.ImageWidth,
                        Height = item.ImageHeight,
                        SourceType = "MANUAL",
                        ConfidenceScore = null,
                        OriginalText = null,
                        CreatedAtUtc = now,
                        CreatedByUserId = plan.ActorUserId,
                        UpdatedAtUtc = null,
                        UpdatedByUserId = null
                    };

                    newRegions.Add(newRegion);
                    result[item.ChapterPageVersionId] = newRegion;
                }
            }

            if (newRegions.Count > 0)
            {
                _context.PageRegions.AddRange(newRegions);
            }

            return result;
        }

        private static string BuildAuditDetailJson(
            QuickSelectAssignmentPlanItem item, QuickSelectAssignmentPlan plan, Guid fullPageRegionId)
        {
            var detail = new Dictionary<string, object>
            {
                ["assigned_to_user_id"] = plan.AssignedToUserId.ToString(),
                ["type_code"] = plan.TypeCode,
                ["task_title"] = item.FinalTaskTitle,
                ["priority_level"] = (int)plan.PriorityLevel,
                ["due_at_utc"] = plan.DueAtUtc.ToString("o"),
                ["compensation_amount"] = plan.CompensationAmount,
                ["page_region_ids"] = new[] { fullPageRegionId.ToString() }
            };

            return JsonSerializer.Serialize(detail);
        }
    }
}
