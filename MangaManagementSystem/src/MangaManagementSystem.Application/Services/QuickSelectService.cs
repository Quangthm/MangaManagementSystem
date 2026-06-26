using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;

namespace MangaManagementSystem.Application.Services
{
    public class QuickSelectService : IQuickSelectService
    {
        private static readonly HashSet<string> ValidTaskTypeCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "BACKGROUND", "SHADING", "EFFECTS", "CLEANUP",
            "DIALOGUE", "TYPESETTING", "REVIEW", "OTHER"
        };

        private readonly IQuickSelectRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IImageMetadataProvider _imageMetadataProvider;

        public QuickSelectService(
            IQuickSelectRepository repository,
            IUnitOfWork unitOfWork,
            IImageMetadataProvider imageMetadataProvider)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _imageMetadataProvider = imageMetadataProvider;
        }

        public async Task<IReadOnlyList<QuickSelectChapterDto>> GetQuickSelectChaptersAsync(
            Guid actorUserId, Guid seriesId, CancellationToken cancellationToken = default)
        {
            if (actorUserId == Guid.Empty)
                throw new InvalidOperationException("Actor user ID is required.");

            if (seriesId == Guid.Empty)
                throw new InvalidOperationException("Series ID is required.");

            await ValidateActorIsActiveMangakaContributorAsync(actorUserId, seriesId, cancellationToken);

            return await _repository.GetQuickSelectChaptersAsync(seriesId, cancellationToken);
        }

        public async Task<IReadOnlyList<QuickSelectPageDto>> GetQuickSelectPagesAsync(
            Guid chapterId, CancellationToken cancellationToken = default)
        {
            if (chapterId == Guid.Empty)
                throw new InvalidOperationException("Chapter ID is required.");

            return await _repository.GetQuickSelectPagesAsync(chapterId, cancellationToken);
        }

        public async Task<IReadOnlyList<QuickSelectAssistantDto>> GetQuickSelectAssistantsAsync(
            Guid actorUserId, Guid seriesId, CancellationToken cancellationToken = default)
        {
            if (actorUserId == Guid.Empty)
                throw new InvalidOperationException("Actor user ID is required.");

            if (seriesId == Guid.Empty)
                throw new InvalidOperationException("Series ID is required.");

            await ValidateActorIsActiveMangakaContributorAsync(actorUserId, seriesId, cancellationToken);

            return await _repository.GetQuickSelectAssistantsAsync(seriesId, cancellationToken);
        }

        public async Task<QuickSelectTaskAssignmentResult> AssignQuickSelectTasksAsync(
            Guid actorUserId,
            QuickSelectTaskAssignmentRequest request,
            CancellationToken cancellationToken = default)
        {
            ValidateRequestBasics(actorUserId, request);

            await ValidateActorIsActiveMangakaContributorAsync(actorUserId, request.SeriesId, cancellationToken);

            await ValidateAssignedUserIsActiveAssistantContributorAsync(
                request.AssignedToUserId, request.SeriesId, cancellationToken);

            var chapter = await _unitOfWork.Chapters.GetByIdAsync(request.ChapterId);
            if (chapter == null || chapter.SeriesId != request.SeriesId)
                throw new InvalidOperationException("Selected chapter does not belong to this series.");

            var (pageVersions, pageFileMap, pageNoMap) =
                await LoadAndValidatePagesAsync(request, cancellationToken);

            // Resolve image bounds for each page version BEFORE opening a transaction
            var boundsMap = new Dictionary<Guid, (int Width, int Height)>();
            foreach (var cpvId in pageVersions.Keys)
            {
                var fileId = pageFileMap[cpvId];
                var fileResource = await _unitOfWork.FileResources.GetByIdAsync(fileId);
                if (fileResource == null || string.IsNullOrWhiteSpace(fileResource.CloudinaryPublicId))
                    throw new InvalidOperationException(
                        "Selected page image dimensions could not be loaded. No tasks were created.");

                var bounds = await _imageMetadataProvider.GetImageBoundsAsync(
                    fileResource.CloudinaryPublicId, cancellationToken);

                if (bounds == null || bounds.Width <= 0 || bounds.Height <= 0)
                    throw new InvalidOperationException(
                        "Selected page image dimensions could not be loaded. No tasks were created.");

                boundsMap[cpvId] = (bounds.Width, bounds.Height);
            }

            var now = DateTime.UtcNow;
            var items = new List<QuickSelectAssignmentPlanItem>();

            foreach (var pr in request.Pages)
            {
                var cpv = pageVersions[pr.ChapterPageVersionId];
                var fileId = pageFileMap[pr.ChapterPageVersionId];
                var bounds = boundsMap[pr.ChapterPageVersionId];
                var fileResource = await _unitOfWork.FileResources.GetByIdAsync(fileId);

                var taskId = Guid.NewGuid();
                var pageNo = pageNoMap[pr.ChapterPageId];
                var descriptionOverride = pr.DescriptionOverride?.Trim();
                var finalDescription = !string.IsNullOrWhiteSpace(descriptionOverride)
                    ? descriptionOverride
                    : request.DefaultTaskDescription.Trim();
                var finalTitle = $"{request.TaskTitlePrefix.Trim()} - Page {pageNo}";

                items.Add(new QuickSelectAssignmentPlanItem(
                    ChapterPageTaskId: taskId,
                    ChapterPageId: pr.ChapterPageId,
                    ChapterPageVersionId: pr.ChapterPageVersionId,
                    PageNo: pageNo,
                    VersionNo: cpv.VersionNo,
                    PageFileResourceId: fileId,
                    CloudinaryPublicId: fileResource!.CloudinaryPublicId,
                    ImageWidth: bounds.Width,
                    ImageHeight: bounds.Height,
                    ExistingFullPageRegionId: null,
                    FinalTaskTitle: finalTitle,
                    FinalTaskDescription: finalDescription,
                    UsedDescriptionOverride: descriptionOverride
                ));
            }

            var plan = new QuickSelectAssignmentPlan(
                ActorUserId: actorUserId,
                SeriesId: request.SeriesId,
                ChapterId: request.ChapterId,
                AssignedToUserId: request.AssignedToUserId,
                TypeCode: request.TypeCode.Trim().ToUpperInvariant(),
                TaskTitlePrefix: request.TaskTitlePrefix.Trim(),
                PriorityLevel: request.PriorityLevel,
                DueAtUtc: request.DueAtUtc,
                CompensationAmount: request.CompensationAmount,
                Items: items
            );

            return await _repository.PersistQuickSelectAssignmentAsync(plan, cancellationToken);
        }

        private static void ValidateRequestBasics(Guid actorUserId, QuickSelectTaskAssignmentRequest request)
        {
            if (actorUserId == Guid.Empty)
                throw new InvalidOperationException("Actor user ID is required.");

            if (request.SeriesId == Guid.Empty)
                throw new InvalidOperationException("Series ID is required.");

            if (request.ChapterId == Guid.Empty)
                throw new InvalidOperationException("Chapter ID is required.");

            if (request.AssignedToUserId == Guid.Empty)
                throw new InvalidOperationException("Assigned user ID is required.");

            if (request.Pages == null || request.Pages.Count == 0)
                throw new InvalidOperationException("No pages selected.");

            var pageIds = request.Pages.Select(p => p.ChapterPageId).ToList();
            if (pageIds.Distinct().Count() != pageIds.Count)
                throw new InvalidOperationException("Duplicate pages are not allowed.");

            var versionIds = request.Pages.Select(p => p.ChapterPageVersionId).ToList();
            if (versionIds.Distinct().Count() != versionIds.Count)
                throw new InvalidOperationException("Duplicate page versions are not allowed.");

            var typeCode = request.TypeCode?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(typeCode) || !ValidTaskTypeCodes.Contains(typeCode))
                throw new InvalidOperationException("Invalid task type.");

            if (string.IsNullOrWhiteSpace(request.TaskTitlePrefix))
                throw new InvalidOperationException("Task title prefix is required.");

            if (string.IsNullOrWhiteSpace(request.DefaultTaskDescription))
                throw new InvalidOperationException("Default task description is required.");

            if (request.PriorityLevel < 1 || request.PriorityLevel > 5)
                throw new InvalidOperationException("Priority level must be between 1 and 5.");

            if (request.DueAtUtc == default)
                throw new InvalidOperationException("Due date is required.");

            if (request.CompensationAmount < 0)
                throw new InvalidOperationException("Compensation amount must not be negative.");
        }

        private async Task<(Dictionary<Guid, ChapterPageVersion> Versions,
                             Dictionary<Guid, Guid> PageFileMap,
                             Dictionary<Guid, int> PageNoMap)>
            LoadAndValidatePagesAsync(QuickSelectTaskAssignmentRequest request, CancellationToken ct)
        {
            var versionDict = new Dictionary<Guid, ChapterPageVersion>();
            var pageFileMap = new Dictionary<Guid, Guid>();
            var pageNoMap = new Dictionary<Guid, int>();

            foreach (var pr in request.Pages)
            {
                var cpv = await _unitOfWork.ChapterPageVersions.GetByIdAsync(pr.ChapterPageVersionId);
                if (cpv == null || cpv.ChapterPageId != pr.ChapterPageId)
                    throw new InvalidOperationException(
                        "Selected page version does not belong to the selected page.");

                if (!cpv.IsCurrentVersion)
                    throw new InvalidOperationException("Selected page version is no longer current.");

                var chapterPage = await _unitOfWork.ChapterPages.GetByIdAsync(pr.ChapterPageId);
                if (chapterPage == null || chapterPage.ChapterId != request.ChapterId)
                    throw new InvalidOperationException(
                        "Selected page does not belong to the selected chapter.");

                versionDict[pr.ChapterPageVersionId] = cpv;
                pageFileMap[pr.ChapterPageVersionId] = cpv.PageFileId;
                pageNoMap[pr.ChapterPageId] = chapterPage.PageNo;
            }

            return (versionDict, pageFileMap, pageNoMap);
        }

        private async Task ValidateActorIsActiveMangakaContributorAsync(
            Guid actorUserId, Guid seriesId, CancellationToken ct)
        {
            var contributors = await _unitOfWork.SeriesContributors.GetAllAsync();
            var isContributor = contributors.Any(sc =>
                sc.UserId == actorUserId &&
                sc.SeriesId == seriesId &&
                sc.EndDate == null);

            if (!isContributor)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(actorUserId);
                var userStatus = user?.StatusCode;
                var roleId = user?.RoleId;

                if (user == null || userStatus != "ACTIVE" || roleId == Guid.Empty)
                    throw new InvalidOperationException(
                        "You no longer have permission to assign tasks for this series.");

                throw new InvalidOperationException(
                    "You no longer have permission to assign tasks for this series.");
            }
        }

        private async Task ValidateAssignedUserIsActiveAssistantContributorAsync(
            Guid assignedUserId, Guid seriesId, CancellationToken ct)
        {
            var contributors = await _unitOfWork.SeriesContributors.GetAllAsync();
            var isContributor = contributors.Any(sc =>
                sc.UserId == assignedUserId &&
                sc.SeriesId == seriesId &&
                sc.EndDate == null);

            if (!isContributor)
                throw new InvalidOperationException(
                    "Assistant is no longer an active contributor for this series.");
        }
    }
}
