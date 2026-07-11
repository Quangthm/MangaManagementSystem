using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MudBlazor;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Web.Services;
using MangaManagementSystem.Web.Services.Api;

namespace MangaManagementSystem.Web.Components.Pages.Mangaka;

public partial class CreatorWorkspace
{
    private bool _shouldRender = true;

    protected override bool ShouldRender()
        => _shouldRender;

    protected override void OnAfterRender(bool firstRender)
    {
        _shouldRender = false;
    }

    private void NotifyStateChanged()
    {
        _shouldRender = true;
        StateHasChanged();
    }

    [Parameter] public string Slug { get; set; } = string.Empty;
    [SupplyParameterFromQuery] public string? chapterId { get; set; }
    [SupplyParameterFromQuery] public string? returnUrl { get; set; }
    [SupplyParameterFromQuery] public string? taskId { get; set; }

    private string? SeriesId { get; set; }
    private bool _accessDenied = false;
    private Guid? _currentUserId;
    private string _currentRoleName = "";

    // Split View
    private bool _isSplitView = false;
    private int _splitPageIndex = 0;
    private List<PageModel> _splitUploadedPages = new();

    // AutoSave
    private bool _isAutoSaving = false;
    private System.Threading.Timer? _autoSaveTimer;
    private readonly SemaphoreSlim _dbLock = new SemaphoreSlim(1, 1);
    private readonly SemaphoreSlim _autoSaveLock = new SemaphoreSlim(1, 1);
    private readonly SemaphoreSlim _uploadLock = new SemaphoreSlim(1, 1);
    private CancellationTokenSource? _operationCts;

    // Versions panel collapse state (default collapsed to save canvas space)
    private bool _versionsCollapsedLeft = true;
    private bool _versionsCollapsedRight = true;

    // Collapse the NEW TASK form to give the ACTIVE TASKS list more room (laptop screens)
    private bool _newTaskFormCollapsed = false;

    // Collapse the NEW ANNOTATION form to give the PAGE ISSUES list more room
    private bool _newAnnotationFormCollapsed = false;

    private bool _seriesNotFound = false;
    private bool _isAddingChapter = false;
    private bool _isLoadingChapter = false;
    private string SeriesTitle { get; set; } = "Loading...";
    private string SeriesSubtitle { get; set; } = "";
    private int SelectedChapter = 0;
    private int SelectedPage = 0;

    private Guid? _taskTargetVersionId;
    private string? _taskFilterId;
    private bool _isRightPanelOpen = true;

    // Assistant mode
    private bool _isAssistantMode;
    private ChapterPageTaskDto? _activeAssistantTask;
    private IBrowserFile? _selectedSubmitFile;
    private string _versionNotes = "";
    private bool _isSubmittingTask;

    /// <summary>
    /// Context-aware back-navigation URL. When the workspace is opened from the Editor chapter
    /// review flow with a safe returnUrl, the back arrow returns there. Otherwise it falls back
    /// to the series page.
    /// </summary>
    private string BackHref
    {
        get
        {
            if (SafeReturnUrl.IsSafe(returnUrl))
            {
                if (returnUrl!.StartsWith("/series/", StringComparison.OrdinalIgnoreCase))
                    return returnUrl!;
                return SafeReturnUrl.AppendReturnUrl($"/series/{Slug}", returnUrl);
            }
            return $"/series/{Slug}";
        }
    }

    private List<RegionModel> SelectedRegions = new();


    private string GetRelativeTime(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;
        if (timeSpan.TotalSeconds < 60) return "just now";
        if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes} min ago";
        if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours} hours ago";
        return $"{timeSpan.Days} days ago";
    }




    private string TaskType { get; set; } = "BACKGROUND";
    private Guid? AssignedAssistantId { get; set; }
    private string TaskDescription { get; set; } = "";

    private void OnTaskTypeChanged(string value) => TaskType = value;
    private void OnAssistantChanged(Guid? value) => AssignedAssistantId = value;

    // Task logic
    public class ProductionTask
    {
        public int Id { get; set; }
        public Guid? DbId { get; set; }
        public string Type { get; set; } = "";
        public string Target { get; set; } = "";
        public string Assistant { get; set; } = "";
        public string Description { get; set; } = "";
        public string Status { get; set; } = "Todo";
        public List<RegionModel> Regions { get; set; } = new();
    }

    private List<ProductionTask> ActiveTasks = new();

    private async Task<List<Guid>> EnsureRegionsSavedAsync(IEnumerable<RegionModel> regionsToSave)
    {
        var savedIds = new List<Guid>();
        if (UploadedPages.Count <= ActivePageIndex) return savedIds;

        var page = UploadedPages[ActivePageIndex];
        if (!page.Versions.Any()) return savedIds;

        var version = page.Versions[page.ActiveVersionIndex];
        bool updatedAny = false;

        foreach (var region in regionsToSave)
        {
            if (region.DbId == null)
            {
                string label = !string.IsNullOrEmpty(region.Label) ? region.Label : $"Region_{region.Id}";
                var newRegionDto = new CreatePageRegionDto(
                    ChapterPageVersionId: version.ChapterPageVersionId,
                    TypeCode: region.Type,
                    RegionLabel: label,
                    X: (decimal)region.X,
                    Y: (decimal)region.Y,
                    Width: (decimal)region.Width,
                    Height: (decimal)region.Height,
                    ConfidenceScore: null,
                    SourceType: "MANUAL",
                    OriginalText: region.OriginalText,
                    PageRegionId: region.DbId
                );

                var dbRegion = await RegionService.CreatePageRegionAsync(newRegionDto);
                region.DbId = dbRegion.PageRegionId;
                updatedAny = true;
            }
            savedIds.Add(region.DbId.Value);
        }

        if (updatedAny)
        {
            var options = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };
            var allRegions = System.Text.Json.JsonSerializer.Deserialize<List<RegionModel>>(version.Regions ?? "[]", options);
            if (allRegions != null)
            {
                foreach (var r in allRegions)
                {
                    var matched = regionsToSave.FirstOrDefault(x => x.Id == r.Id);
                    if (matched != null) r.DbId = matched.DbId;
                }
                version.Regions = System.Text.Json.JsonSerializer.Serialize(allRegions, options);
                if (GetActiveCanvas() != null)
                {
                    await GetActiveCanvas()!.InvokeVoidAsync("loadRegions", version.Regions);
                }
            }
        }
        return savedIds;
    }

    private async Task CreateTask()
    {
        if (IsChapterLocked) return;
        if (AssignedAssistantId == null)
        {
            Snackbar.Add("Please select an assistant to assign.", Severity.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(TaskDescription))
        {
            Snackbar.Add("Please enter a task description.", Severity.Warning);
            return;
        }

        var regionsToSave = SelectedRegions.ToList();
        if (!regionsToSave.Any())
        {
            regionsToSave.Add(new RegionModel
            {
                Type = "OTHER",
                X = 0,
                Y = 0,
                Width = 0.01,
                Height = 0.01
            });
        }
        var regionIds = await EnsureRegionsSavedAsync(regionsToSave);

        int newId = ActiveTasks.Any() ? ActiveTasks.Max(t => t.Id) + 1 : 1;

        string target;
        if (SelectedRegions.Any())
        {
            var targets = SelectedRegions.Select(r => $"Panel {r.Id} [X:{Math.Round(r.X)}, Y:{Math.Round(r.Y)}, W:{Math.Round(r.Width)}, H:{Math.Round(r.Height)}]");
            target = string.Join("\n", targets);
        }
        else
        {
            target = $"Page {SelectedPage}";
        }

        await _dbLock.WaitAsync();
        try
        {
            var taskDto = new CreateChapterPageTaskDto(
                ActorUserId: _currentUserId.Value,
                AssignedToUserId: AssignedAssistantId.Value,
                TypeCode: TaskType,
                StatusCode: "TODO",
                TaskTitle: $"{TaskType} Task for {target}",
                TaskDescription: TaskDescription,
                PriorityLevel: 2,
                DueAtUtc: null,
                CompensationAmount: null,
                CompletedPageVersionId: null,
                PageRegionIds: regionIds
            );

            var dbTask = await TaskService.CreateChapterPageTaskAsync(taskDto);

            var assistantName = _assistantUsers.FirstOrDefault(u => u.UserId == AssignedAssistantId.Value)?.Username ?? AssignedAssistantId.ToString();

            ActiveTasks.Insert(0, new ProductionTask
            {
                Id = newId,
                DbId = dbTask.ChapterPageTaskId,
                Type = TaskType,
                Assistant = assistantName,
                Target = target,
                Description = TaskDescription,
                Status = "Todo"
            });
            TaskDescription = ""; // Reset form
            Snackbar.Add("Task assigned successfully!", Severity.Success);
            NotifyStateChanged();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error creating task: {ex.Message}", Severity.Error);
        }
        finally
        {
            _dbLock.Release();
        }
    }

    private async Task SubmitAssistantWorkAsync()
    {
        if (_selectedSubmitFile == null || _activeAssistantTask == null) return;

        _isSubmittingTask = true;
        try
        {
            // 1. Read file bytes (no lock needed — local memory)
            using var stream = _selectedSubmitFile.OpenReadStream(maxAllowedSize: 1024 * 1024 * 20);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            // 2. Upload to Cloudinary (upload lock only — isolates network I/O from DB)
            FileUploadResultDto uploadResult;
            await _uploadLock.WaitAsync();
            try
            {
                uploadResult = await FileStorageService.UploadFileAsync(
                    fileBytes,
                    _selectedSubmitFile.Name,
                    _selectedSubmitFile.ContentType,
                    "CHAPTER_PAGE_VERSION");
            }
            finally
            {
                _uploadLock.Release();
            }

            // 3. Submit task under DB lock
            await _dbLock.WaitAsync();
            try
            {
                var request = new AssistantTaskSubmitRequestDto(
                    ActorUserId: _currentUserId!.Value,
                    ChapterPageTaskId: _activeAssistantTask.ChapterPageTaskId,
                    StorageProviderCode: "CLOUDINARY",
                    PublicId: uploadResult.PublicId,
                    SecureUrl: uploadResult.SecureUrl,
                    OriginalFileName: uploadResult.OriginalFileName,
                    ContentType: uploadResult.ContentType,
                    FileSizeBytes: uploadResult.FileSizeBytes,
                    Sha256Hash: uploadResult.Sha256Hash,
                    VersionNote: string.IsNullOrWhiteSpace(_versionNotes) ? null : _versionNotes);

                var result = await AssistantTaskSubmissionService.SubmitTaskWorkAsync(request);

                _activeAssistantTask = _activeAssistantTask with { StatusCode = "UNDER_REVIEW" };
                _selectedSubmitFile = null;
                _versionNotes = "";
                Snackbar.Add("Work submitted for review successfully!", Severity.Success);
                NotifyStateChanged();

                if (ActivePageIndex >= 0 && ActivePageIndex < UploadedPages.Count)
                {
                    await LoadPage(ActivePageIndex);
                }
            }
            finally
            {
                _dbLock.Release();
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to submit work: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSubmittingTask = false;
            NotifyStateChanged();
        }
    }

    private async Task CycleTaskStatus(ProductionTask task)
    {
        if (task.Status == "In Progress" || task.Status == "Todo")
        {
            task.Status = task.Status == "Todo" ? "In Progress" : "Todo";
            Snackbar.Add("Status updated. Tasks can only be moved to Review/Done by assistants submitting work.", Severity.Info);
        }
        else
        {
            Snackbar.Add("Cannot manually cycle Review/Done status. Assistants must submit work.", Severity.Warning);
            return;
        }

        if (task.DbId.HasValue)
        {
            try
            {
                await _dbLock.WaitAsync();
                var existingTask = await TaskService.GetChapterPageTaskByIdAsync(task.DbId.Value);
                if (existingTask != null)
                {
                    var statusCodeDb = task.Status switch
                    {
                        "Todo" => "ASSIGNED",
                        "In Progress" => "ASSIGNED",
                        "Review" => "UNDER_REVIEW",
                        "Done" => "COMPLETED",
                        _ => "ASSIGNED"
                    };

                    var updateDto = new Application.DTOs.Manga.UpdateChapterPageTaskDto(
                        existingTask.ChapterPageTaskId,
                        existingTask.AssignedToUserId,
                        existingTask.TypeCode,
                        statusCodeDb,
                        existingTask.TaskTitle,
                        existingTask.TaskDescription,
                        existingTask.PriorityLevel,
                        existingTask.DueAtUtc,
                        existingTask.CompensationAmount,
                        existingTask.CompletedPageVersionId,
                        existingTask.PageRegions.Select(r => r.PageRegionId).ToList()
                    );

                    await TaskService.UpdateChapterPageTaskAsync(updateDto);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error updating task status in DB: {ex.Message}", Severity.Error);
            }
            finally
            {
                _dbLock.Release();
            }
        }

        NotifyStateChanged();
    }

    private async Task DeleteTask(int taskId)
    {
        if (IsChapterLocked) return;
        bool? result = await DialogService.ShowMessageBox(
            "Delete Task",
            "Are you sure you want to delete this task?",
            yesText: "Delete", cancelText: "Cancel");

        if (result == true)
        {
            await _dbLock.WaitAsync();
            try
            {
                var task = ActiveTasks.FirstOrDefault(t => t.Id == taskId);
                if (task != null)
                {
                    if (task.DbId.HasValue)
                    {
                        await TaskService.CancelTaskAsync(_currentUserId!.Value, task.DbId.Value, "Deleted by user");
                    }
                    ActiveTasks.Remove(task);
                    NotifyStateChanged();
                }
            }
            finally
            {
                _dbLock.Release();
            }
        }
    }

    // Annotation logic
    public class AnnotationModel
    {
        public int Id { get; set; }
        public Guid? DbId { get; set; }
        public string Type { get; set; } = "";
        public string Comment { get; set; } = "";
        public string Target { get; set; } = "";
        public string Author { get; set; } = "Editor";
        public int PageNumber { get; set; } = 1;
        public bool IsResolved { get; set; } = false;
        public double? PinX { get; set; }
        public double? PinY { get; set; }
        public List<RegionModel> Regions { get; set; } = new();
    }

    private string AnnotationType { get; set; } = "TYPESETTING_ERROR";
    private string AnnotationComment { get; set; } = "";
    private double? PendingPinX = null;
    private double? PendingPinY = null;

    [JSInvokable]
    public void OnPinAdded(string pane, double x, double y)
    {
        PendingPinX = x;
        PendingPinY = y;
        SelectedRegions.Clear();
        NotifyStateChanged();
    }

    [JSInvokable]
    public void OnToolChangedFromJS(string pane, string tool)
    {
        CurrentTool = tool;
        NotifyStateChanged();
    }

    private List<AnnotationModel> ActiveAnnotations = new();
    private bool _showAnnotationsOnCanvas = true;

    private async Task ToggleCanvasAnnotations()
    {
        _showAnnotationsOnCanvas = !_showAnnotationsOnCanvas;
        var canvas = _activePane == "Left" ? _leftCanvasRef : _rightCanvasRef;
        if (canvas != null)
        {
            if (_showAnnotationsOnCanvas)
            {
                var currentAnnotations = ActiveAnnotations.Where(a => a.PageNumber == SelectedPage && a.PinX.HasValue && a.PinY.HasValue);
                await canvas.InvokeVoidAsync("syncAnnotations", currentAnnotations.Select(a => new { pinX = a.PinX.Value, pinY = a.PinY.Value, isResolved = a.IsResolved }));
            }
            else
            {
                await canvas.InvokeVoidAsync("syncAnnotations", Array.Empty<object>());
            }
        }
    }

    private async Task SaveVersionNote()
    {
        if (!UploadedPages.Any()) return;
        var activePage = UploadedPages[ActivePageIndex];
        if (!activePage.Versions.Any()) return;
        var activeVer = activePage.Versions[activePage.ActiveVersionIndex];

        try
        {
            var dbVer = await VersionService.GetChapterPageVersionByIdAsync(activeVer.ChapterPageVersionId);
            if (dbVer != null)
            {
                await VersionService.UpdateChapterPageVersionAsync(new UpdateChapterPageVersionDto(
                    ChapterPageVersionId: dbVer.ChapterPageVersionId,
                    ChapterPageId: dbVer.ChapterPageId,
                    VersionNo: dbVer.VersionNo,
                    PageFileId: dbVer.PageFileId,
                    VersionNote: activeVer.Note,
                    IsCurrentVersion: dbVer.IsCurrentVersion
                ));
                Snackbar.Add("Version note saved.", Severity.Success);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error saving note: {ex.Message}", Severity.Error);
        }
    }

    private async Task CreateAnnotation()
    {
        if (IsChapterLocked) return;
        if (string.IsNullOrWhiteSpace(AnnotationComment))
        {
            Snackbar.Add("Please enter a comment.", Severity.Warning);
            return;
        }

        var regionsToSave = SelectedRegions.ToList();
        if (PendingPinX.HasValue && PendingPinY.HasValue)
        {
            regionsToSave.Add(new RegionModel
            {
                Type = "OTHER",
                X = PendingPinX.Value,
                Y = PendingPinY.Value,
                Width = 0.01,
                Height = 0.01
            });
        }
        else if (!regionsToSave.Any())
        {
            regionsToSave.Add(new RegionModel
            {
                Type = "OTHER",
                X = 0,
                Y = 0,
                Width = 0.01,
                Height = 0.01
            });
        }

        await _dbLock.WaitAsync();
        try
        {
            var regionIds = await EnsureRegionsSavedAsync(regionsToSave);

            int newId = ActiveAnnotations.Any() ? ActiveAnnotations.Max(t => t.Id) + 1 : 1;

            string target;
            if (PendingPinX.HasValue && PendingPinY.HasValue)
                target = $"Pin at ({Math.Round(PendingPinX.Value)}, {Math.Round(PendingPinY.Value)})";
            else if (SelectedRegions.Any())
            {
                var targets = SelectedRegions.Select(r => $"Panel {r.Id} [X:{Math.Round(r.X)}, Y:{Math.Round(r.Y)}, W:{Math.Round(r.Width)}, H:{Math.Round(r.Height)}]");
                target = string.Join("\n", targets);
            }
            else
                target = $"Whole Page ({SelectedPage})";

            var annDto = new CreateChapterPageAnnotationDto(
                IssueTypeCode: AnnotationType,
                AnnotatedByUserId: _currentUserId.Value,
                AnnotationText: AnnotationComment,
                PageRegionIds: regionIds
            );

            var dbAnn = await AnnotationService.CreateChapterPageAnnotationAsync(annDto);

            ActiveAnnotations.Insert(0, new AnnotationModel
            {
                Id = newId,
                DbId = dbAnn.ChapterPageAnnotationId,
                Type = AnnotationType,
                Comment = AnnotationComment,
                Target = target,
                PageNumber = SelectedPage,
                PinX = PendingPinX,
                PinY = PendingPinY
            });
            AnnotationComment = "";
            PendingPinX = null;
            PendingPinY = null;
            Snackbar.Add("Annotation added successfully!", Severity.Success);
            NotifyStateChanged();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error creating annotation: {ex.Message}", Severity.Error);
        }
        finally
        {
            _dbLock.Release();
        }
        if (GetActiveCanvas() != null)
        {
            var pageAnnotations = ActiveAnnotations.Where(a => a.PageNumber == SelectedPage || a.PageNumber == 0).ToList();
            await GetActiveCanvas()!.InvokeVoidAsync("syncAnnotations", pageAnnotations);
        }
    }

    private async Task SyncAnnotationsToJS()
    {
        if (GetActiveCanvas() != null)
        {
            var pageAnnotations = ActiveAnnotations.Where(a => a.PageNumber == SelectedPage || a.PageNumber == 0).ToList();
            await GetActiveCanvas()!.InvokeVoidAsync("syncAnnotations", pageAnnotations);
        }
    }

    private async Task ResolveAnnotation(int id)
    {
        if (IsChapterLocked) return;
        var ann = ActiveAnnotations.FirstOrDefault(a => a.Id == id);
        if (ann != null)
        {
            ann.IsResolved = true;
            if (ann.DbId.HasValue && _currentUserId.HasValue)
            {
                try
                {
                    await AnnotationService.ResolveAnnotationAsync(_currentUserId.Value, ann.DbId.Value);
                    Snackbar.Add("Issue resolved.", Severity.Success);
                }
                catch (Exception ex)
                {
                    Snackbar.Add($"Error resolving issue: {ex.Message}", Severity.Error);
                }
            }
            else
            {
                Snackbar.Add("Issue resolved locally.", Severity.Success);
            }
            await SyncAnnotationsToJS();
            NotifyStateChanged();
        }
    }

    private async Task DeleteAnnotation(int id)
    {
        if (IsChapterLocked) return;
        bool? result = await DialogService.ShowMessageBox(
            "Delete Annotation",
            "Are you sure you want to delete this annotation?",
            yesText: "Delete", cancelText: "Cancel");

        if (result == true)
        {
            var ann = ActiveAnnotations.FirstOrDefault(a => a.Id == id);
            if (ann != null)
            {
                if (ann.DbId.HasValue)
                {
                    await AnnotationService.DeleteChapterPageAnnotationAsync(ann.DbId.Value);
                }
                ActiveAnnotations.Remove(ann);
                await SyncAnnotationsToJS();
                NotifyStateChanged();
            }
        }
    }

    public class ChapterModel
    {
        public int Id { get; set; }
        public Guid ChapterId { get; set; }
        public int PageCount { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsPending { get; set; }
        public string StatusCode { get; set; } = "DRAFT";
        public string Title { get; set; } = "";
        public bool IsRenaming { get; set; } = false;
        public List<PageModel> Pages { get; set; } = new();
        public bool IsPagesLoaded { get; set; } = false;
    }

    private List<ChapterModel> Chapters = new();

    private List<Application.DTOs.Auth.UserDto> _assistantUsers = new();

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        // Resolve actor user ID from auth state.
        try
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var idClaim = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(idClaim, out var uid))
                _currentUserId = uid;
            var roleClaim = authState.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            _currentRoleName = roleClaim ?? "";
        }
        catch { }

        // Workspace access guard: resolve slug -> SeriesId and verify series-specific access.
        if (string.IsNullOrWhiteSpace(Slug) || !_currentUserId.HasValue)
        {
            _accessDenied = true;
            return;
        }

        try
        {
            var entry = await SeriesApiClient.GetWorkspaceEntryAsync(_currentUserId.Value, Slug);
            if (entry is null || !entry.CanAccess)
            {
                _accessDenied = true;
                return;
            }

            SeriesId = entry.SeriesId.ToString();
        }
        catch
        {
            _accessDenied = true;
            return;
        }

        // Fetch series data, chapters, and contributors in parallel
        Task<IEnumerable<SeriesContributorDto>>? contributorsTask = null;
        Task<SeriesDto?>? seriesTask = null;
        Task<IEnumerable<ChapterDto>>? chaptersTask = null;

        if (Guid.TryParse(SeriesId, out var sId))
        {
            contributorsTask = ContributorService.GetSeriesContributorsBySeriesIdAsync(sId);
            seriesTask = SeriesService.GetSeriesByIdAsync(sId);
            chaptersTask = ChapterService.GetChaptersBySeriesIdAsync(sId);

            await Task.WhenAll(contributorsTask, seriesTask, chaptersTask);
        }
        else
        {
            _accessDenied = true;
            return;
        }

        // Handle contributor results
        try
        {
            if (contributorsTask != null)
            {
                var contributors = await contributorsTask;
                var contributorUserIds = contributors.Select(c => c.UserId).Distinct().ToList();
                var allUsers = await UserService.GetUsersByIdsAsync(contributorUserIds);
                _assistantUsers = allUsers
                    .Where(u => string.Equals(u.RoleName, "Assistant", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }
        catch (Exception)
        {
            _assistantUsers = new List<Application.DTOs.Auth.UserDto>();
        }

        // Handle series results
        try
        {
            if (seriesTask != null)
            {
                var series = await seriesTask;
                if (series != null)
                {
                    SeriesTitle = series.Title;
                    SeriesSubtitle = string.Join(", ", series.Genres.Select(g => g.GenreName));
                }
            }
        }
        catch { }

        // Handle chapters results
        IEnumerable<ChapterDto>? chapters = null;
        if (chaptersTask != null)
            chapters = await chaptersTask;
        if (chapters != null && chapters.Any())
        {
            var chapterModels = chapters
                .Where(c => c.StatusCode != "CANCELLED")
                .Select((c, i) => new ChapterModel
                {
                    Id = int.TryParse(c.ChapterNumberLabel, out int num) ? num : (i + 1),
                    ChapterId = c.ChapterId,
                    PageCount = 0,
                    IsCompleted = c.StatusCode == "PUBLISHED",
                    StatusCode = c.StatusCode,
                    Title = c.ChapterTitle ?? ""
                }).OrderBy(c => c.Id).ToList();

            var pageCountsDict = await PageService.GetPageCountsByChapterIdsAsync(chapterModels.Select(c => c.ChapterId));
            foreach (var cm in chapterModels)
            {
                if (pageCountsDict.TryGetValue(cm.ChapterId, out int cnt))
                    cm.PageCount = cnt;
            }

            Chapters = chapterModels;

            if (Chapters.Any())
            {
                Guid? resolveChapterId = null;

                if (!string.IsNullOrWhiteSpace(taskId) && Guid.TryParse(taskId, out var parsedTaskId))
                {
                    try
                    {
                        if (_currentRoleName == "Assistant")
                        {
                            var task = await TaskService.GetAssignedTaskDetailForAssistantAsync(_currentUserId.Value, parsedTaskId);
                            if (task != null)
                            {
                                _isAssistantMode = true;
                                _activeAssistantTask = task;
                                resolveChapterId = task.ChapterId;
                                _taskTargetVersionId = task.SourceChapterPageVersionId;
                                _taskFilterId = taskId;
                            }
                        }
                        else
                        {
                            var task = await TaskService.GetChapterPageTaskByIdAsync(parsedTaskId);
                            if (task != null)
                            {
                                resolveChapterId = task.ChapterId;
                                _taskTargetVersionId = task.SourceChapterPageVersionId;
                                _taskFilterId = taskId;
                            }
                        }
                    }
                    catch
                    {
                        // fall through to default chapter selection
                    }
                }

                if (resolveChapterId.HasValue)
                {
                    var targetChapter = Chapters.FirstOrDefault(c => c.ChapterId == resolveChapterId.Value);
                    if (targetChapter != null)
                    {
                        await SelectChapter(targetChapter.Id);
                    }
                    else
                    {
                        await SelectChapter(Chapters.First().Id);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(chapterId) && Guid.TryParse(chapterId, out var targetChapterId))
                {
                    var targetChapter = Chapters.FirstOrDefault(c => c.ChapterId == targetChapterId);
                    if (targetChapter != null)
                    {
                        await SelectChapter(targetChapter.Id);
                    }
                    else
                    {
                        await SelectChapter(Chapters.First().Id);
                    }
                }
                else
                {
                    await SelectChapter(Chapters.First().Id);
                }
            }
        }
        else
        {
            Chapters = new();
        }
        _seriesNotFound = false;
        return;
        _seriesNotFound = true;
    }

    private async Task SelectChapter(int chapterId)
    {
        if (_isLoadingChapter) return;

        SelectedChapter = chapterId;
        try
        {
            ActivePageIndex = 0;
            SelectedPage = 0; // Reset selected page to avoid UI crashes

            var chapter = Chapters.FirstOrDefault(c => c.Id == chapterId);
            if (chapter != null && !chapter.IsPagesLoaded)
            {
                await _dbLock.WaitAsync();
                try
                {
                    _isLoadingChapter = true;
                    NotifyStateChanged();
                    var pages = (await PageService.GetChapterPagesByChapterIdAsync(chapter.ChapterId)).ToList();
                    var pageIds = pages.Select(p => p.ChapterPageId).Distinct();
                    var allVersions = (await VersionService.GetChapterPageVersionsByPageIdsAsync(pageIds)).ToList();

                    var fileIds = allVersions.Select(v => v.PageFileId).Where(fid => fid != Guid.Empty).Distinct();
                    var filesDict = (await FileResourceService.GetFileResourcesByIdsAsync(fileIds)).ToDictionary(f => f.FileResourceId);

                    var verIds = allVersions.Select(v => v.ChapterPageVersionId).Distinct().ToList();

                    // Guard against pathological region counts (corrupt / exploded segmentation
                    // data, e.g. a page with hundreds of thousands of regions). Only materialize
                    // regions for versions within a sane cap so one bad page cannot freeze the
                    // entire chapter load. Over-cap versions load with no regions but remain
                    // viewable and deletable.
                    const int MaxRegionsPerVersion = 2000;
                    var regionCounts = await RegionService.GetRegionCountsByVersionIdsAsync(verIds);
                    var safeVerIds = verIds
                        .Where(vid => !regionCounts.TryGetValue(vid, out var cnt) || cnt <= MaxRegionsPerVersion)
                        .ToList();
                    bool anyVersionCapped = safeVerIds.Count < verIds.Count;
                    var regionsGrouped = (await RegionService.GetPageRegionsByVersionIdsAsync(safeVerIds)).ToLookup(r => r.ChapterPageVersionId);

                    var versionsGrouped = allVersions.ToLookup(v => v.ChapterPageId);

                    foreach (var p in pages)
                    {
                        var pageModel = new PageModel { ChapterPageId = p.ChapterPageId };
                        var versions = versionsGrouped[p.ChapterPageId];
                        foreach (var v in versions)
                        {
                            filesDict.TryGetValue(v.PageFileId, out var file);
                            var dbRegions = regionsGrouped[v.ChapterPageVersionId];

                            var mappedRegions = new List<RegionModel>();
                            int idx = 1;
                            foreach (var r in dbRegions)
                            {
                                // Skip pin regions (tiny markers that anchor annotations). They
                                // are rendered by the annotation pin layer, not as region boxes —
                                // otherwise they show up as a duplicate "#N" box on the page.
                                if (r.Width <= 0.05m && r.Height <= 0.05m) continue;

                                string origText = r.OriginalText ?? "";
                                string transText = "";

                                if (origText.TrimStart().StartsWith("{"))
                                {
                                    try
                                    {
                                        var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(origText);
                                        if (dict != null)
                                        {
                                            if (dict.ContainsKey("original")) origText = dict["original"];
                                            if (dict.ContainsKey("translated")) transText = dict["translated"];
                                        }
                                    }
                                    catch { }
                                }

                                mappedRegions.Add(new RegionModel
                                {
                                    Id = idx++,
                                    DbId = r.PageRegionId,
                                    Label = r.RegionLabel,
                                    Type = r.TypeCode,
                                    X = (double)r.X,
                                    Y = (double)r.Y,
                                    Width = (double)r.Width,
                                    Height = (double)r.Height,
                                    OriginalText = origText,
                                    TranslatedText = transText
                                });
                            }

                            string regionsJson = mappedRegions.Any()
                                ? System.Text.Json.JsonSerializer.Serialize(mappedRegions, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase })
                                : "[]";

                            pageModel.Versions.Add(new PageVersionModel
                            {
                                VersionNo = v.VersionNo,
                                ChapterPageVersionId = v.ChapterPageVersionId,
                                Note = v.VersionNote ?? "",
                                DataUrl = file?.CloudinarySecureUrl ?? "",
                                Regions = regionsJson,
                                IsCurrentVersion = v.IsCurrentVersion,
                                IsDeleted = (v.PageFileId == Guid.Empty || file == null || file.DeletedAtUtc != null)
                            });
                        }
                        var activeIdx = pageModel.Versions.FindIndex(v => v.IsCurrentVersion && !v.IsDeleted);
                        if (activeIdx >= 0) pageModel.ActiveVersionIndex = activeIdx;
                        chapter.Pages.Add(pageModel);
                    }
                    chapter.PageCount = chapter.Pages.Count;
                    chapter.IsPagesLoaded = true;
                    if (anyVersionCapped)
                    {
                        Snackbar.Add("Một số trang có số lượng vùng (region) bất thường nên tạm bỏ qua khi tải để tránh treo. Hãy xóa trang đó để dọn dữ liệu lỗi.", Severity.Warning);
                    }
                }
                finally
                {
                    _isLoadingChapter = false;
                    _dbLock.Release();
                    NotifyStateChanged();
                }
            }

            if (UploadedPages.Any())
            {
                int targetIndex = 0;
                if (_taskTargetVersionId.HasValue)
                {
                    for (int i = 0; i < UploadedPages.Count; i++)
                    {
                        var page = UploadedPages[i];
                        if (page.Versions.Any(v => v.ChapterPageVersionId == _taskTargetVersionId.Value))
                        {
                            targetIndex = i;
                            break;
                        }
                    }
                }
                await LoadPage(targetIndex);
            }
            else
            {
                if (_leftCanvasRef != null)
                    await _leftCanvasRef.InvokeVoidAsync("loadImage", "");
                if (_rightCanvasRef != null)
                    await _rightCanvasRef.InvokeVoidAsync("loadImage", "");
            }
            // Sync split view pane when chapter changes. Updating the data alone does not
            // re-render the right canvas (it is JS-rendered), so drive it through
            // OnSplitPageChanged to actually load the new chapter's page into the right pane.
            if (_isSplitView)
            {
                _splitUploadedPages = UploadedPages.ToList();
                if (_splitUploadedPages.Any())
                {
                    await OnSplitPageChanged(1);
                }
                else
                {
                    _splitPageIndex = 0;
                }
            }
            NotifyStateChanged();
        }
        catch (TaskCanceledException)
        {
            // Ignore cancelled tasks during re-rendering or fast switching
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading chapter: {ex.Message}", Severity.Error);
            Console.WriteLine(ex.ToString());
        }
    }

    private async Task SubmitChapterForReview(Guid chapterId)
    {
        var chapter = Chapters.FirstOrDefault(c => c.ChapterId == chapterId);
        if (chapter != null)
        {
            try
            {
                await ChapterService.UpdateChapterStatusAsync(chapterId, "UNDER_REVIEW");
                chapter.StatusCode = "UNDER_REVIEW";
                chapter.IsCompleted = false; // Just to be safe since it's mapped to PUBLISHED originally
                NotifyStateChanged();
                Snackbar.Add("Chapter submitted for review.", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Failed to submit: {ex.Message}", Severity.Error);
            }
        }
    }

    private async Task CancelSubmission(Guid chapterId)
    {
        var chapter = Chapters.FirstOrDefault(c => c.ChapterId == chapterId);
        if (chapter != null)
        {
            try
            {
                await ChapterService.UpdateChapterStatusAsync(chapterId, "DRAFT");
                chapter.StatusCode = "DRAFT";
                NotifyStateChanged();
                Snackbar.Add("Submission cancelled. Chapter is now back to DRAFT.", Severity.Info);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Failed to cancel submission: {ex.Message}", Severity.Error);
            }
        }
    }

    private async Task AddNewChapter()
    {
        if (_isAddingChapter) return;

        try
        {
            _isAddingChapter = true;
            NotifyStateChanged();

            if (Guid.TryParse(SeriesId, out Guid seriesGuid))
            {
                // Chapter numbers must be unique across ALL chapters in the series,
                // including CANCELLED ones: uq_chapter_series_chapter_number is NOT a
                // filtered index, and the workspace hides cancelled chapters. Computing
                // the next number from the visible list alone collided with a hidden
                // cancelled chapter ("duplicate key (series, 1)"). Use the full list.
                var allChapters = await ChapterService.GetChaptersBySeriesIdAsync(seriesGuid);
                int maxExisting = allChapters
                    .Select(c => int.TryParse(c.ChapterNumberLabel, out var n) ? n : 0)
                    .DefaultIfEmpty(0)
                    .Max();
                int newId = Math.Max(maxExisting, Chapters.Any() ? Chapters.Max(c => c.Id) : 0) + 1;

                var dbChapter = await ChapterService.CreateChapterAsync(new CreateChapterDto(
                    SeriesId: seriesGuid,
                    ChapterNumberLabel: newId.ToString(),
                    ChapterTitle: null,
                    StatusCode: "DRAFT",
                    PlannedReleaseDate: null
                ));

                Chapters.Add(new ChapterModel { Id = newId, ChapterId = dbChapter.ChapterId, PageCount = 0, IsCompleted = false, Title = "" });
                await SelectChapter(newId);
                Snackbar.Add($"Created Chapter {newId}", Severity.Success);
            }
            else
            {
                Snackbar.Add("Could not create chapter: Invalid Series ID", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to create chapter: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isAddingChapter = false;
            NotifyStateChanged();
        }
    }

    private async Task DeleteChapter(int chapterId)
    {
        bool? result = await DialogService.ShowMessageBox(
            "Delete Chapter",
            "Are you sure you want to delete this chapter?",
            yesText: "Delete", cancelText: "Cancel");

        if (result == true)
        {
            var chapter = Chapters.FirstOrDefault(c => c.Id == chapterId);
            if (chapter != null)
            {
                if (chapter.ChapterId != Guid.Empty)
                {
                    // Only the author's own draft chapters are truly deletable. A chapter
                    // that is under or past editorial review (or already CANCELLED, which is
                    // terminal) must not be hard-deleted here — cancellation goes through the
                    // editorial review workflow. This removes the old "delete = set CANCELLED"
                    // behaviour that was leaving hidden cancelled chapters behind.
                    var status = chapter.StatusCode;
                    if (status != "DRAFT" && status != "REVISION_REQUESTED")
                    {
                        Snackbar.Add("Chỉ xóa được chapter ở trạng thái nháp (DRAFT/REVISION_REQUESTED). Chapter đang/đã review phải được hủy qua quy trình duyệt biên tập.", Severity.Warning);
                        return;
                    }

                    try
                    {
                        await ChapterService.DeleteChapterAsync(chapter.ChapterId);
                    }
                    catch (Exception ex)
                    {
                        Snackbar.Add($"Failed to delete chapter: {ex.Message}", Severity.Error);
                        return;
                    }
                }

                Chapters.Remove(chapter);
                if (SelectedChapter == chapterId)
                {
                    if (Chapters.Any())
                    {
                        await SelectChapter(Chapters.First().Id);
                    }
                    else
                    {
                        SelectedChapter = 0;
                        ActivePageIndex = 0;
                        if (GetActiveCanvas() != null)
                        {
                            await GetActiveCanvas()!.InvokeVoidAsync("loadImage", "");
                        }
                    }
                }
                NotifyStateChanged();
            }
        }
    }

    private void PromptRenameChapter(ChapterModel chapter)
    {
        chapter.IsRenaming = true;
    }

    private async Task SaveChapterName(ChapterModel chapter)
    {
        if (chapter.ChapterId != Guid.Empty)
        {
            try
            {
                await ChapterService.UpdateChapterTitleAsync(chapter.ChapterId, chapter.Title);
                Snackbar.Add("Chapter renamed successfully.", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error renaming chapter: {ex.Message}", Severity.Error);
            }
        }
        chapter.IsRenaming = false;
    }

    private async Task HandleRenameKeyDown(KeyboardEventArgs e, ChapterModel chapter)
    {
        if (e.Key == "Enter")
        {
            await SaveChapterName(chapter);
        }
        else if (e.Key == "Escape")
        {
            CancelRenameChapter(chapter);
        }
    }

    private void CancelRenameChapter(ChapterModel chapter)
    {
        // Revert title? We don't have the old title saved unless we fetch it.
        // It's fine to just close it for now.
        chapter.IsRenaming = false;
    }

    // Multi-page logic
    public class PageVersionModel
    {
        public int VersionNo { get; set; }
        public string DataUrl { get; set; } = "";
        public string Note { get; set; } = "";
        public string? Regions { get; set; } = null;
        public bool IsDirty { get; set; } = false;
        public Guid ChapterPageVersionId { get; set; }
        public bool IsCurrentVersion { get; set; } = false;
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class PageModel
    {
        public Guid ChapterPageId { get; set; }
        public List<PageVersionModel> Versions { get; set; } = new();
        public int ActiveVersionIndex { get; set; } = 0;
    }

    private List<PageModel> UploadedPages
    {
        get
        {
            var chap = Chapters.FirstOrDefault(c => c.Id == SelectedChapter);
            return chap != null ? chap.Pages : new List<PageModel>();
        }
    }
    private int ActivePageIndex = 0;

    private bool IsChapterLocked
    {
        get
        {
            var chap = Chapters.FirstOrDefault(c => c.Id == SelectedChapter);
            if (chap == null) return false;
            var code = chap.StatusCode;
            return code == "UNDER_REVIEW" || code == "APPROVED" || code == "SCHEDULED" || code == "RELEASED" || code == "CANCELLED";
        }
    }

    // AI Canvas logic
    private IJSObjectReference _moduleFactory = null!;
    private IJSObjectReference _leftCanvasRef = null!;
    private IJSObjectReference _rightCanvasRef = null!;
    private DotNetObjectReference<CanvasInterop>? _objRefLeft;
    private DotNetObjectReference<CanvasInterop>? _objRefRight;
    private string _activePane = "Left";

    private void SetActivePane(string pane)
    {
        if (_activePane != pane)
        {
            _activePane = pane;
            SelectedPage = pane == "Left" ? ActivePageIndex + 1 : _splitPageIndex + 1;

            // Sync SelectedRegions for the newly active pane
            SelectedRegions.Clear();
            var page = pane == "Left" ? UploadedPages.ElementAtOrDefault(ActivePageIndex) : _splitUploadedPages.ElementAtOrDefault(_splitPageIndex);
            if (page != null && page.Versions.Any())
            {
                var regionsStr = page.Versions[page.ActiveVersionIndex].Regions;
                if (!string.IsNullOrEmpty(regionsStr))
                {
                    try
                    {
                        var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var allRegions = System.Text.Json.JsonSerializer.Deserialize<List<RegionModel>>(regionsStr, options);
                        if (allRegions != null)
                        {
                            SelectedRegions = allRegions.Where(r => r.Selected).ToList();
                        }
                    }
                    catch { }
                }
            }
            NotifyStateChanged();
        }
    }

    private IJSObjectReference? GetActiveCanvas() => _activePane == "Left" ? _leftCanvasRef : _rightCanvasRef;

    public class CanvasInterop
    {
        private readonly CreatorWorkspace _parent;
        public string Pane { get; }
        public CanvasInterop(CreatorWorkspace parent, string pane) { _parent = parent; Pane = pane; }

        [JSInvokable]
        public void OnPinAdded(double x, double y) => _parent.OnPinAdded(Pane, x, y);

        [JSInvokable]
        public void OnToolChangedFromJS(string newTool) => _parent.OnToolChangedFromJS(Pane, newTool);

        [JSInvokable]
        public void OnRegionsUpdated(string regionsJson, bool canUndo, bool canRedo) => _parent.OnRegionsUpdated(Pane, regionsJson, canUndo, canRedo);

        [JSInvokable]
        public Task<string> SegmentImageJS(string base64Image) => _parent.SegmentImageJS(Pane, base64Image);

        [JSInvokable]
        public Task<string> TranslateRegionsJS(string payloadJson) => _parent.TranslateRegionsJS(Pane, payloadJson);
    }

    private bool IsProcessing = false;
    private string CurrentTool = "select";
    private bool CanUndo = false;
    private bool CanRedo = false;

    // Brush settings
    private string BrushColor { get; set; } = "#ffffff";
    private int BrushSize { get; set; } = 20;

    private async Task OnBrushColorChanged(ChangeEventArgs e)
    {
        BrushColor = e.Value?.ToString() ?? "#ffffff";
        await GetActiveCanvas()!.InvokeVoidAsync("setBrushSettings", BrushColor, BrushSize);
    }

    private async Task OnBrushSizeChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int size))
        {
            BrushSize = size;
            await GetActiveCanvas()!.InvokeVoidAsync("setBrushSettings", BrushColor, BrushSize);
        }
    }

    private DotNetObjectReference<CreatorWorkspace>? _objRef;

    private async Task SaveProgress()
    {
        var pagesToSave = UploadedPages.ToList();
        if (_isSplitView && _splitUploadedPages != null)
        {
            pagesToSave = pagesToSave.Union(_splitUploadedPages).ToList();
        }

        // Nothing edited → skip the DB round-trip, the semaphore, and the toast
        // entirely. This makes navigating between unedited pages effectively free.
        bool anyDirty = pagesToSave.Any(p => p != null
            && p.Versions.Any()
            && p.Versions[p.ActiveVersionIndex].IsDirty);
        if (!anyDirty) return;

        int savedCount = 0;
        await _dbLock.WaitAsync();
        try
        {
            foreach (var page in pagesToSave)
            {
                if (page == null) continue;
                if (page.Versions.Any())
                {
                    var currentVersion = page.Versions[page.ActiveVersionIndex];
                    if (!currentVersion.IsDirty) continue;
                    if (!string.IsNullOrEmpty(currentVersion.Regions))
                    {
                        try
                        {
                            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                            var allRegions = System.Text.Json.JsonSerializer.Deserialize<List<RegionModel>>(currentVersion.Regions, options);

                            if (allRegions != null)
                            {
                                var dtos = allRegions.Select(r =>
                                {
                                    // Serialize text as JSON to safely store translations without DB schema changes
                                    var textJson = System.Text.Json.JsonSerializer.Serialize(new
                                    {
                                        original = r.OriginalText ?? "",
                                        translated = r.TranslatedText ?? ""
                                    });

                                    string label = !string.IsNullOrEmpty(r.Label) ? r.Label : $"Region_{r.Id}";
                                    return new MangaManagementSystem.Application.DTOs.Manga.CreatePageRegionDto(
                                        currentVersion.ChapterPageVersionId,
                                        (r.Type ?? "OTHER").ToUpper(),
                                        label,
                                        (decimal)r.X,
                                        (decimal)r.Y,
                                        (decimal)r.Width,
                                        (decimal)r.Height,
                                        null,
                                        "MANUAL",
                                        textJson,
                                        r.DbId
                                    );
                                }).ToList();

                                await RegionService.BulkReplacePageRegionsAsync(currentVersion.ChapterPageVersionId, dtos);
                                currentVersion.IsDirty = false;
                                savedCount++;
                                try { await JS.InvokeVoidAsync("clearMmsDraft", page.ChapterPageId.ToString()); } catch { }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error saving regions: {ex.Message}");
                        }
                    }
                }
            }
            if (savedCount > 0)
            {
                Snackbar.Add("Progress saved successfully!", Severity.Success);
            }
        }
        finally
        {
            _dbLock.Release();
        }
    }

    private async Task HandleBackClick()
    {
        await SaveProgress();
        Nav.NavigateTo(BackHref);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await SaveProgress();
        }
        catch (JSDisconnectedException)
        {
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task ExportCurrentPage()
    {
        var canvas = GetActiveCanvas();
        if (canvas == null || UploadedPages.Count == 0 || ActivePageIndex < 0 || ActivePageIndex >= UploadedPages.Count)
        {
            Snackbar.Add("Không có trang nào để xuất.", Severity.Warning);
            return;
        }

        var page = UploadedPages[ActivePageIndex];
        if (!page.Versions.Any())
        {
            Snackbar.Add("Trang này chưa có ảnh để xuất.", Severity.Warning);
            return;
        }

        try
        {
            // Build a tidy, unambiguous file name so the user's Downloads folder stays organised.
            var slugPart = string.IsNullOrWhiteSpace(Slug) ? "page" : Slug;
            var safeSlug = new string(slugPart.Select(ch => char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' ? ch : '-').ToArray());
            var fileName = $"{safeSlug}_ch{SelectedChapter}_page{SelectedPage}.png";

            // Renders the finished page (clean background + translated text, no editor boxes) and
            // downloads it to the user's machine. Nothing is written to the database/Cloudinary.
            var ok = await canvas.InvokeAsync<bool>("downloadRenderedImage", fileName);
            if (ok)
            {
                Snackbar.Add($"Đã xuất ảnh: {fileName}", Severity.Success);
            }
            else
            {
                Snackbar.Add("Không xuất được trang (ảnh có thể chưa tải xong). Hãy thử lại.", Severity.Warning);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Xuất ảnh thất bại: {ex.Message}", Severity.Error);
            Console.WriteLine(ex.ToString());
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var version = DateTime.Now.Ticks;
            _moduleFactory = await JS.InvokeAsync<IJSObjectReference>("import", $"/js/mangaAiCanvas.js?v={version}");
            _leftCanvasRef = await _moduleFactory.InvokeAsync<IJSObjectReference>("createMangaCanvasInstance");
            _rightCanvasRef = await _moduleFactory.InvokeAsync<IJSObjectReference>("createMangaCanvasInstance");
            _objRefLeft = DotNetObjectReference.Create(new CanvasInterop(this, "Left"));
            _objRefRight = DotNetObjectReference.Create(new CanvasInterop(this, "Right"));
            await _leftCanvasRef.InvokeVoidAsync("initCanvas", "ai-canvas-left", "ai-canvas-container-left", _objRefLeft);
            await _rightCanvasRef.InvokeVoidAsync("initCanvas", "ai-canvas-right", "ai-canvas-container-right", _objRefRight);

            if (UploadedPages.Any())
            {
                await LoadPage(ActivePageIndex);
            }
            else
            {
                // Wait for user to upload an image.
                Snackbar.Add("Please upload an image to begin.", Severity.Info);
            }
        }
    }

    private async Task HandleFileUpload(InputFileChangeEventArgs e)
    {
        if (IsChapterLocked) return;
        try
        {
            if (SelectedChapter == 0)
            {
                if (!Chapters.Any())
                {
                    await AddNewChapter();
                }
                else
                {
                    Snackbar.Add("Please select a chapter first.", Severity.Warning);
                    return;
                }
            }

            var files = e.GetMultipleFiles(20);
            if (files.Any())
            {
                ChapterModel? chap = null;

                // Phase 1: Upload each file to Cloudinary one at a time (streaming, no batch byte[] in RAM)
                var uploadResults = new List<(FileUploadResultDto Result, string Name, string ContentType)>();
                await _uploadLock.WaitAsync();
                try
                {
                    foreach (var file in files)
                    {
                        await using var stream = file.OpenReadStream(maxAllowedSize: 1024 * 1024 * 20);
                        var result = await FileStorageService.UploadFileAsync(
                            stream, file.Name, file.ContentType, "CHAPTER_PAGE_VERSION");
                        uploadResults.Add((result, file.Name, file.ContentType));
                    }
                }
                finally
                {
                    _uploadLock.Release();
                }

                // Phase 2: DB operations (db lock only)
                await _dbLock.WaitAsync();
                try
                {
                    var activeChap = Chapters.FirstOrDefault(c => c.Id == SelectedChapter);
                    int maxPageNo = 0;
                    if (activeChap != null)
                    {
                        var existingDbPages = await PageService.GetChapterPagesByChapterIdAsync(activeChap.ChapterId);
                        maxPageNo = existingDbPages.Any() ? existingDbPages.Max(p => p.PageNo) : 0;
                    }

                    foreach (var (uploadResult, fileName, contentType) in uploadResults)
                    {
                        if (activeChap != null)
                        {
                            // 1. Create File Resource DB entry
                            var fileResource = await FileResourceService.CreateFileResourceAsync(new CreateFileResourceDto(
                                FilePurposeCode: "CHAPTER_PAGE_VERSION",
                                OriginalFileName: fileName,
                                CloudinaryPublicId: uploadResult.PublicId,
                                CloudinarySecureUrl: uploadResult.SecureUrl,
                                ContentType: contentType,
                                FileSizeBytes: uploadResult.FileSizeBytes,
                                Sha256Hash: uploadResult.Sha256Hash,
                                UploadedByUserId: _currentUserId
                            ));

                            // 2. Create Chapter Page
                            maxPageNo++;
                            var dbPage = await PageService.CreateChapterPageAsync(new CreateChapterPageDto(
                                ChapterId: activeChap.ChapterId,
                                PageNo: maxPageNo,
                                PageNotes: null
                            ));

                            // 3. Create Chapter Page Version
                            var dbVersion = await VersionService.CreateChapterPageVersionAsync(new CreateChapterPageVersionDto(
                                ChapterPageId: dbPage.ChapterPageId,
                                VersionNo: 1,
                                PageFileId: fileResource.FileResourceId,
                                VersionNote: "Original Upload"
                            ));

                            await VersionService.SetCurrentVersionAsync(dbPage.ChapterPageId, dbVersion.ChapterPageVersionId);

                            var newPage = new PageModel { ChapterPageId = dbPage.ChapterPageId };
                            newPage.Versions.Add(new PageVersionModel
                            {
                                VersionNo = 1,
                                DataUrl = uploadResult.SecureUrl,
                                Note = "Original Upload",
                                ChapterPageVersionId = dbVersion.ChapterPageVersionId,
                                IsCurrentVersion = true
                            });
                            activeChap.Pages.Add(newPage);
                        }
                    }
                }
                finally
                {
                    _dbLock.Release();
                }

                chap = Chapters.FirstOrDefault(c => c.Id == SelectedChapter);
                if (chap != null) chap.PageCount = chap.Pages.Count;

                NotifyStateChanged();
                await Task.Delay(1);

                int newPageIndex = chap != null ? Math.Max(0, chap.Pages.Count - files.Count) : ActivePageIndex;
                await LoadPage(newPageIndex);
                Snackbar.Add($"Loaded {files.Count} page(s) successfully!", Severity.Success);
            }
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            Snackbar.Add($"Error uploading: {msg}", Severity.Error);
            Console.WriteLine(ex.ToString());
        }
    }

    private async Task OnSplitPageChanged(int pageNumber)
    {
        if (_isPageLoading) return;
        _isPageLoading = true;
        try
        {
            _splitPageIndex = pageNumber - 1;
            if (_rightCanvasRef != null && _splitUploadedPages.Any())
            {
                var page = _splitUploadedPages[_splitPageIndex];
                if (page.Versions.Any())
                {
                    var v = page.Versions[page.ActiveVersionIndex];
                    await _rightCanvasRef.InvokeVoidAsync("loadImage", OptimizedImageUrl(v.DataUrl));

                    // Programmatic load: silent=true to avoid a phantom change echo.
                    await _rightCanvasRef.InvokeVoidAsync("loadRegions",
                        string.IsNullOrEmpty(v.Regions) ? "[]" : v.Regions, true);

                    var shadowDraft = await JS.InvokeAsync<string>("getMmsDraft", page.ChapterPageId.ToString());
                    if (HasMeaningfulDraft(shadowDraft, v.Regions))
                    {
                        Snackbar.Add($"⚠️ Unsaved draft detected for the right page.", Severity.Warning, config =>
                        {
                            config.Action = "Restore";
                            config.ActionColor = Color.Primary;
                            config.OnClick = snackbar =>
                            {
                                _ = _rightCanvasRef.InvokeVoidAsync("loadRegions", shadowDraft).AsTask();
                                return Task.CompletedTask;
                            };
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading split page: {ex.Message}");
        }
        finally
        {
            _isPageLoading = false;
            // Re-render after clearing the guard. The initial load runs from
            // OnAfterRenderAsync (which does NOT auto-render afterwards), so without this
            // the UI kept rendering _isPageLoading = true and the pagination stayed
            // visually Disabled forever after a reload.
            NotifyStateChanged();
        }
    }

    private async Task OnPageChanged(int pageNumber)
    {
        if (_isPageLoading) return;
        try
        {
            await SaveProgress();
            await LoadPage(pageNumber - 1);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error changing page: {ex.Message}");
        }
    }

    // Re-entrancy guard for page navigation (see OnPageChanged / LoadPage).
    private bool _isPageLoading = false;

    // Per-page cache of tasks & annotations (keyed by ChapterPageId) so revisiting a
    // data-heavy page does not re-query the DB. The UI mutates these same list
    // instances (Insert/Remove/property edits), so the cache stays in sync in-session.
    private readonly Dictionary<Guid, (List<ProductionTask> Tasks, List<AnnotationModel> Annotations)> _pageDataCache = new();

    /// <summary>
    /// Returns the regions JSON with the volatile UI-only "selected" flag removed,
    /// so content comparisons ignore selection (an ephemeral, non-persisted) noise.
    /// </summary>
    private static string StripSelected(string? regionsJson)
    {
        if (string.IsNullOrWhiteSpace(regionsJson)) return "[]";
        try
        {
            var node = System.Text.Json.Nodes.JsonNode.Parse(regionsJson);
            if (node is System.Text.Json.Nodes.JsonArray arr)
            {
                foreach (var item in arr)
                {
                    if (item is System.Text.Json.Nodes.JsonObject obj)
                    {
                        obj.Remove("selected");
                        obj.Remove("Selected");
                    }
                }
                return arr.ToJsonString();
            }
        }
        catch { }
        return regionsJson.Trim();
    }

    /// <summary>
    /// True only when a stored shadow draft differs in real content from the saved
    /// regions. Prevents false "unsaved draft" warnings on plain page navigation.
    /// </summary>
    private static bool HasMeaningfulDraft(string? draft, string? savedRegions)
    {
        if (string.IsNullOrWhiteSpace(draft)) return false;
        return StripSelected(draft) != StripSelected(savedRegions);
    }

    /// <summary>
    /// Requests a bandwidth-optimized rendition (auto format + auto quality) from
    /// Cloudinary WITHOUT any resize, so pixel dimensions — and therefore region
    /// coordinates — stay identical. Returns the original URL unchanged if it is not
    /// a recognizable Cloudinary delivery URL or is already transformed.
    /// </summary>
    private static string OptimizedImageUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return url ?? "";
        const string marker = "/upload/";
        int idx = url.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return url;
        int insertAt = idx + marker.Length;
        if (url.AsSpan(insertAt).StartsWith("f_auto")) return url;
        return url.Insert(insertAt, "f_auto,q_auto/");
    }

    private async Task LoadPage(int index)
    {
        if (index < 0 || index >= UploadedPages.Count) return;
        if (_isPageLoading) return;
        _isPageLoading = true;
        try
        {
            var page = UploadedPages[index];

            ActivePageIndex = index;
            SelectedPage = index + 1; // update the header number

            if (page.Versions.Any() && _leftCanvasRef != null)
            {
                var activeVersion = page.Versions[page.ActiveVersionIndex];
                await _leftCanvasRef.InvokeVoidAsync("loadImage", OptimizedImageUrl(activeVersion.DataUrl));

                // Programmatic load: silent=true so the canvas does not echo a
                // phantom change back to Blazor. A freshly loaded page has no undo.
                await _leftCanvasRef.InvokeVoidAsync("loadRegions",
                    string.IsNullOrEmpty(activeVersion.Regions) ? "[]" : activeVersion.Regions, true);
                CanUndo = false;
                CanRedo = false;

                // Warn only when a stored shadow draft really differs from the
                // saved regions (ignores selection-only and empty drafts).
                var shadowDraft = await JS.InvokeAsync<string>("getMmsDraft", page.ChapterPageId.ToString());
                if (HasMeaningfulDraft(shadowDraft, activeVersion.Regions))
                {
                    Snackbar.Add($"⚠️ Unsaved draft detected for this page.", Severity.Warning, config =>
                    {
                        config.Action = "Khôi phục";
                        config.ActionColor = Color.Primary;
                        config.OnClick = snackbar =>
                        {
                            _ = _leftCanvasRef.InvokeVoidAsync("loadRegions", shadowDraft).AsTask();
                            return Task.CompletedTask;
                        };
                    });
                }
            }

            // Tasks & annotations are scoped to the ChapterPage (not the version), so
            // cache them per page and only hit the DB on a cache miss. Revisiting a page
            // (or switching versions) then needs no extra round-trip.
            if (_pageDataCache.TryGetValue(page.ChapterPageId, out var cached))
            {
                ActiveTasks = cached.Tasks;
                ActiveAnnotations = cached.Annotations;
            }
            else
            {
                var newTasks = new List<ProductionTask>();
                var newAnnotations = new List<AnnotationModel>();
                bool loadedOk = false;
                try
                {
                    await _dbLock.WaitAsync();
                    try
                    {
                        var dbTasks = await TaskService.GetChapterPageTasksByChapterPageIdAsync(page.ChapterPageId);
                        int idCounter = 1;
                        foreach (var t in dbTasks)
                        {
                            var statusMap = t.StatusCode switch
                            {
                                "ASSIGNED" => "Todo",
                                "IN_PROGRESS" => "In Progress",
                                "UNDER_REVIEW" => "Review",
                                "COMPLETED" => "Done",
                                _ => "Todo"
                            };
                            newTasks.Add(new ProductionTask
                            {
                                Id = idCounter++,
                                DbId = t.ChapterPageTaskId,
                                Type = t.TypeCode,
                                Assistant = t.AssignedUsername ?? t.AssignedToDisplayName ?? t.AssignedToUserId.ToString(),
                                Target = $"Page {SelectedPage}",
                                Description = t.TaskDescription,
                                Status = statusMap,
                                Regions = t.PageRegions?.Select(r => new RegionModel
                                {
                                    Id = ParseRegionId(r.RegionLabel),
                                    DbId = r.PageRegionId,
                                    Type = r.TypeCode,
                                    X = (double)r.X,
                                    Y = (double)r.Y,
                                    Width = (double)r.Width,
                                    Height = (double)r.Height
                                }).ToList() ?? new List<RegionModel>()
                            });
                        }

                        var dbAnnotations = await AnnotationService.GetChapterPageAnnotationsByChapterPageIdAsync(page.ChapterPageId);
                        int annIdCounter = 1;
                        foreach (var a in dbAnnotations)
                        {
                            var pinRegion = a.PageRegions?.FirstOrDefault(r => r.Width <= 0.05m && r.Height <= 0.05m);
                            var targetRegions = a.PageRegions?.Where(r => r.Width > 0.05m).Select(r => $"Panel [X:{Math.Round(r.X)}, Y:{Math.Round(r.Y)}, W:{Math.Round(r.Width)}, H:{Math.Round(r.Height)}]").ToList() ?? new List<string>();

                            string annTarget;
                            if (pinRegion != null)
                                annTarget = $"Pin at ({Math.Round(pinRegion.X)}, {Math.Round(pinRegion.Y)})";
                            else if (targetRegions.Any())
                                annTarget = string.Join("\n", targetRegions);
                            else
                                annTarget = $"Page {SelectedPage}";

                            newAnnotations.Add(new AnnotationModel
                            {
                                Id = annIdCounter++,
                                DbId = a.ChapterPageAnnotationId,
                                Type = a.IssueTypeCode,
                                Comment = a.AnnotationText,
                                Target = annTarget,
                                PageNumber = SelectedPage,
                                PinX = pinRegion != null ? (double?)pinRegion.X : null,
                                PinY = pinRegion != null ? (double?)pinRegion.Y : null,
                                IsResolved = a.ResolvedByUserId != null,
                                Regions = a.PageRegions?.Select(r => new RegionModel
                                {
                                    Id = ParseRegionId(r.RegionLabel),
                                    DbId = r.PageRegionId,
                                    Type = r.TypeCode,
                                    X = (double)r.X,
                                    Y = (double)r.Y,
                                    Width = (double)r.Width,
                                    Height = (double)r.Height
                                }).ToList() ?? new List<RegionModel>()
                            });
                        }
                        loadedOk = true;
                    }
                    finally
                    {
                        _dbLock.Release();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading tasks/annotations: {ex.Message}");
                }

                ActiveTasks = newTasks;
                ActiveAnnotations = newAnnotations;
                // Only cache a fully successful load so a transient error retries next visit.
                if (loadedOk)
                {
                    _pageDataCache[page.ChapterPageId] = (newTasks, newAnnotations);
                }
            }

            await SyncAnnotationsToJS();
            NotifyStateChanged();

            // Warm the browser cache for the next/previous page images so paging
            // through a chapter feels instant after the first view. Fire-and-forget
            // so it never blocks navigation or holds the load guard.
            _ = PreloadAdjacentPagesAsync(index);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading page: {ex.Message}");
        }
        finally
        {
            _isPageLoading = false;
            // Re-render after clearing the guard. The initial load runs from
            // OnAfterRenderAsync (which does NOT auto-render afterwards), so without this
            // the UI kept rendering _isPageLoading = true and the pagination stayed
            // visually Disabled forever after a reload.
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Preloads the active-version image of the next and previous pages into the
    /// browser cache (matching crossOrigin so the canvas load reuses it).
    /// </summary>
    private async Task PreloadAdjacentPagesAsync(int index)
    {
        try
        {
            var urls = new List<string>();
            void AddUrl(int i)
            {
                if (i >= 0 && i < UploadedPages.Count)
                {
                    var p = UploadedPages[i];
                    if (p.Versions.Any())
                    {
                        var url = OptimizedImageUrl(p.Versions[p.ActiveVersionIndex].DataUrl);
                        if (!string.IsNullOrEmpty(url)) urls.Add(url);
                    }
                }
            }
            AddUrl(index + 1);
            AddUrl(index - 1);
            if (urls.Count > 0)
            {
                await JS.InvokeVoidAsync("mmsPreloadImages", urls);
            }
        }
        catch { }
    }

    private int ParseRegionId(string? regionLabel)
    {
        if (string.IsNullOrEmpty(regionLabel)) return 0;
        var parts = regionLabel.Split('_');
        if (parts.Length > 1 && int.TryParse(parts[1], out int id))
            return id;
        return 0;
    }

    private void ToggleRightPanel()
    {
        _isRightPanelOpen = !_isRightPanelOpen;
        NotifyStateChanged();
    }

    private async Task DeleteCurrentPage()
    {
        if (IsChapterLocked) return;
        if (UploadedPages.Any())
        {
            var pageToDelete = UploadedPages[ActivePageIndex];
            bool? result = await DialogService.ShowMessageBox(
                "Delete Page",
                "Are you sure you want to delete this page?",
                yesText: "Delete", cancelText: "Cancel");

            if (result == true)
            {
                try
                {
                    if (pageToDelete.ChapterPageId != Guid.Empty)
                    {
                        var success = await PageService.DeleteChapterPageAsync(pageToDelete.ChapterPageId, _currentUserId.Value);
                        if (!success)
                        {
                            Snackbar.Add("Failed to delete page from database.", Severity.Error);
                            return;
                        }
                        _pageDataCache.Remove(pageToDelete.ChapterPageId);
                    }

                    UploadedPages.RemoveAt(ActivePageIndex);

                    var activeChap = Chapters.FirstOrDefault(c => c.Id == SelectedChapter);
                    if (activeChap != null) activeChap.PageCount = UploadedPages.Count;

                    if (UploadedPages.Count == 0)
                    {
                        ActivePageIndex = 0;
                        if (_leftCanvasRef != null)
                            await _leftCanvasRef.InvokeVoidAsync("loadImage", ""); // clear canvas
                        Snackbar.Add("All pages deleted.", Severity.Info);
                    }
                    else
                    {
                        if (ActivePageIndex >= UploadedPages.Count)
                            ActivePageIndex = UploadedPages.Count - 1;
                        await LoadPage(ActivePageIndex);
                        Snackbar.Add("Page deleted.", Severity.Success);
                    }
                    NotifyStateChanged();
                }
                catch (Exception ex)
                {
                    Snackbar.Add($"Error deleting page: {ex.Message}", Severity.Error);
                }
            }
        }
    }

    private async Task SwitchVersion(string pane, int versionIndex, bool isDeleting = false)
    {
        var page = pane == "Left" ? UploadedPages.ElementAtOrDefault(ActivePageIndex) : _splitUploadedPages.ElementAtOrDefault(_splitPageIndex);
        var canvas = pane == "Left" ? _leftCanvasRef : _rightCanvasRef;

        if (page == null) return;
        await SaveProgress();

        // Do not overwrite DataUrl in memory with local canvas exports when switching versions

        if (versionIndex >= 0 && versionIndex < page.Versions.Count)
        {
            page.ActiveVersionIndex = versionIndex;
            var activeVersion = page.Versions[versionIndex];
            if (canvas != null)
            {
                if (activeVersion.IsDeleted)
                {
                    await canvas.InvokeVoidAsync("loadImage", ""); // Clear canvas
                    Snackbar.Add("This version image has been deleted.", Severity.Warning);
                }
                else
                {
                    await canvas.InvokeVoidAsync("loadImage", OptimizedImageUrl(activeVersion.DataUrl));
                }

                // Programmatic load: silent=true so switching versions does not mark
                // the page dirty or write a phantom draft.
                await canvas.InvokeVoidAsync("loadRegions",
                    string.IsNullOrEmpty(activeVersion.Regions) ? "[]" : activeVersion.Regions, true);
                CanUndo = false;
                CanRedo = false;
            }
            NotifyStateChanged();
        }
    }

    private async Task SaveAsNewVersion(string pane)
    {
        var page = pane == "Left" ? UploadedPages.ElementAtOrDefault(ActivePageIndex) : _splitUploadedPages.ElementAtOrDefault(_splitPageIndex);
        var canvas = pane == "Left" ? _leftCanvasRef : _rightCanvasRef;

        if (page != null && canvas != null)
        {
            IsProcessing = true;
            NotifyStateChanged();
            Snackbar.Add("Saving new version to cloud...", Severity.Info);

            string? uploadedPublicId = null;
            try
            {
                var dataUrl = await canvas.InvokeAsync<string>("exportImage");

                int nextVersionNo = page.Versions.Any() ? page.Versions.Max(v => v.VersionNo) + 1 : 1;

                var commaIndex = dataUrl.IndexOf(',');
                var base64Data = dataUrl.Substring(commaIndex + 1);
                var bytes = Convert.FromBase64String(base64Data);

                // 1. Upload to Cloudinary FIRST (upload lock only — isolates network I/O from DB).
                FileUploadResultDto uploadResult;
                await _uploadLock.WaitAsync();
                try
                {
                    uploadResult = await FileStorageService.UploadFileAsync(bytes, $"page_{SelectedPage}_v{nextVersionNo}.png", "image/png", "CHAPTER_PAGE_VERSION");
                }
                finally
                {
                    _uploadLock.Release();
                }
                uploadedPublicId = uploadResult.PublicId;

                var currentRegionsJson = await canvas.InvokeAsync<string>("exportRegions");
                var currentRegions = System.Text.Json.JsonSerializer.Deserialize<List<RegionModel>>(currentRegionsJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var fileDto = new CreateFileResourceDto(
                    FilePurposeCode: "CHAPTER_PAGE_VERSION",
                    OriginalFileName: uploadResult.OriginalFileName,
                    CloudinaryPublicId: uploadResult.PublicId,
                    CloudinarySecureUrl: uploadResult.SecureUrl,
                    ContentType: uploadResult.ContentType,
                    FileSizeBytes: uploadResult.FileSizeBytes,
                    Sha256Hash: uploadResult.Sha256Hash,
                    UploadedByUserId: _currentUserId
                );

                var regionDtos = (currentRegions ?? new List<RegionModel>())
                    .Where(r => r.Width > 0.05 || r.Height > 0.05) // exclude tiny pin markers
                    .Select(r =>
                    {
                        var textObj = new Dictionary<string, string>
                        {
                            { "original", r.OriginalText ?? "" },
                            { "translated", r.TranslatedText ?? "" }
                        };
                        return new CreatePageRegionDto(
                            ChapterPageVersionId: Guid.Empty, // assigned inside the service
                            TypeCode: (r.Type ?? "OTHER").ToUpperInvariant(),
                            RegionLabel: null,
                            X: (decimal)r.X,
                            Y: (decimal)r.Y,
                            Width: (decimal)r.Width,
                            Height: (decimal)r.Height,
                            ConfidenceScore: null,
                            SourceType: "MANUAL",
                            OriginalText: System.Text.Json.JsonSerializer.Serialize(textObj)
                        );
                    }).ToList();

                // 2. Atomic create: FileResource + version + regions + set-current (one transaction).
                var versionDto = await VersionService.CreateVersionWithFileAndRegionsAsync(
                    page.ChapterPageId, (short)nextVersionNo, fileDto, $"New Version {nextVersionNo}",
                    regionDtos, setAsCurrent: true);

                // 3. Edit-bleed fix: the in-session edits now belong to the NEW version only. Keep the
                //    previous version pristine by clearing its dirty flag so SaveProgress never writes
                //    those edits back into it.
                var oldVer = page.Versions.ElementAtOrDefault(page.ActiveVersionIndex);
                if (oldVer != null) oldVer.IsDirty = false;

                foreach (var v in page.Versions) v.IsCurrentVersion = false;
                var newVersionModel = new PageVersionModel
                {
                    ChapterPageVersionId = versionDto.ChapterPageVersionId,
                    VersionNo = nextVersionNo,
                    Regions = currentRegionsJson,
                    DataUrl = uploadResult.SecureUrl,
                    Note = $"New Version {nextVersionNo}",
                    IsCurrentVersion = true
                };
                page.Versions.Add(newVersionModel);
                page.ActiveVersionIndex = page.Versions.Count - 1;

                // 4. Show the new version (silent load → no phantom dirty/draft); reset undo history.
                await canvas.InvokeVoidAsync("loadImage", OptimizedImageUrl(uploadResult.SecureUrl));
                await canvas.InvokeVoidAsync("loadRegions", string.IsNullOrEmpty(currentRegionsJson) ? "[]" : currentRegionsJson, true);
                CanUndo = false;
                CanRedo = false;

                Snackbar.Add($"Created New Version {nextVersionNo}", Severity.Success);
            }
            catch (Exception ex)
            {
                // The DB transaction rolled back; the Cloudinary upload (if it succeeded) is now an
                // orphan — best-effort delete so we do not leave a dangling file.
                if (!string.IsNullOrEmpty(uploadedPublicId))
                {
                    try { await FileStorageService.DeleteFileAsync(uploadedPublicId, "image"); } catch { }
                }
                var msg = ex.InnerException?.Message ?? ex.Message;
                Snackbar.Add($"Failed to save version: {msg}", Severity.Error);
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                IsProcessing = false;
                NotifyStateChanged();
            }
        }
    }

    private async Task HandleUploadVersion(InputFileChangeEventArgs e, string pane)
    {
        if (IsChapterLocked) return;
        var page = pane == "Left" ? UploadedPages.ElementAtOrDefault(ActivePageIndex) : _splitUploadedPages.ElementAtOrDefault(_splitPageIndex);
        var canvas = pane == "Left" ? _leftCanvasRef : _rightCanvasRef;

        if (page != null && canvas != null)
        {
            var file = e.File;
            if (file == null) return;

            IsProcessing = true;
            NotifyStateChanged();
            Snackbar.Add("Uploading new version image to cloud...", Severity.Info);

            string? uploadedPublicId = null;
            try
            {
                using var stream = file.OpenReadStream(maxAllowedSize: 1024 * 1024 * 20);
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var bytes = memoryStream.ToArray();
                int nextVersionNo = page.Versions.Any() ? page.Versions.Max(v => v.VersionNo) + 1 : 1;
                var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "image/png" : file.ContentType;

                var uploadResult = await FileStorageService.UploadFileAsync(bytes, file.Name, contentType, "CHAPTER_PAGE_VERSION");
                uploadedPublicId = uploadResult.PublicId;

                var currentRegionsJson = await canvas.InvokeAsync<string>("exportRegions");
                var currentRegions = System.Text.Json.JsonSerializer.Deserialize<List<RegionModel>>(currentRegionsJson, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var fileDto = new CreateFileResourceDto(
                    FilePurposeCode: "CHAPTER_PAGE_VERSION",
                    OriginalFileName: file.Name,
                    CloudinaryPublicId: uploadResult.PublicId,
                    CloudinarySecureUrl: uploadResult.SecureUrl,
                    ContentType: contentType,
                    FileSizeBytes: bytes.Length,
                    Sha256Hash: uploadResult.Sha256Hash,
                    UploadedByUserId: _currentUserId
                );

                var regionDtos = (currentRegions ?? new List<RegionModel>())
                    .Where(r => r.Width > 0.05 || r.Height > 0.05) // exclude tiny pin markers
                    .Select(r =>
                    {
                        var textObj = new Dictionary<string, string>
                        {
                            { "original", r.OriginalText ?? "" },
                            { "translated", r.TranslatedText ?? "" }
                        };
                        return new CreatePageRegionDto(
                            ChapterPageVersionId: Guid.Empty, // assigned inside the service
                            TypeCode: (r.Type ?? "OTHER").ToUpperInvariant(),
                            RegionLabel: null,
                            X: (decimal)r.X,
                            Y: (decimal)r.Y,
                            Width: (decimal)r.Width,
                            Height: (decimal)r.Height,
                            ConfidenceScore: null,
                            SourceType: "MANUAL",
                            OriginalText: System.Text.Json.JsonSerializer.Serialize(textObj)
                        );
                    }).ToList();

                var versionDto = await VersionService.CreateVersionWithFileAndRegionsAsync(
                    page.ChapterPageId, (short)nextVersionNo, fileDto, $"Uploaded Version {nextVersionNo}",
                    regionDtos, setAsCurrent: true);

                var oldVer = page.Versions.ElementAtOrDefault(page.ActiveVersionIndex);
                if (oldVer != null) oldVer.IsDirty = false;

                foreach (var v in page.Versions) v.IsCurrentVersion = false;
                var newVersionModel = new PageVersionModel
                {
                    ChapterPageVersionId = versionDto.ChapterPageVersionId,
                    VersionNo = nextVersionNo,
                    Regions = currentRegionsJson,
                    DataUrl = uploadResult.SecureUrl,
                    Note = $"Uploaded Version {nextVersionNo}",
                    IsCurrentVersion = true
                };
                page.Versions.Add(newVersionModel);
                page.ActiveVersionIndex = page.Versions.Count - 1;

                await canvas.InvokeVoidAsync("loadImage", OptimizedImageUrl(uploadResult.SecureUrl));
                await canvas.InvokeVoidAsync("loadRegions", string.IsNullOrEmpty(currentRegionsJson) ? "[]" : currentRegionsJson, true);
                CanUndo = false;
                CanRedo = false;

                Snackbar.Add($"Uploaded & created Version {nextVersionNo}", Severity.Success);
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(uploadedPublicId))
                {
                    try { await FileStorageService.DeleteFileAsync(uploadedPublicId, "image"); } catch { }
                }
                var msg = ex.InnerException?.Message ?? ex.Message;
                Snackbar.Add($"Failed to upload version: {msg}", Severity.Error);
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                IsProcessing = false;
                NotifyStateChanged();
            }
        }
    }

    private async Task DeleteCurrentVersion(string pane)
    {
        var page = pane == "Left" ? UploadedPages.ElementAtOrDefault(ActivePageIndex) : _splitUploadedPages.ElementAtOrDefault(_splitPageIndex);
        if (page == null || !page.Versions.Any()) return;

        var activeVer = page.Versions[page.ActiveVersionIndex];
        if (activeVer.VersionNo == 1)
        {
            Snackbar.Add("Cannot delete Version 1 (Original).", Severity.Warning);
            return;
        }

        // Simplistic constraint check: If it has regions, it might have tasks.
        // Actually, fetching tasks/annotations here would require additional calls.
        // For now, if regions exist, warn the user.
        bool confirm = await DialogService.ShowMessageBox("Confirm Delete", $"Are you sure you want to delete v{activeVer.VersionNo} image? History placeholder will remain.", yesText: "Delete", cancelText: "Cancel") == true;
        if (!confirm) return;

        IsProcessing = true;
        NotifyStateChanged();

        try
        {
            var dbVersion = await VersionService.GetChapterPageVersionByIdAsync(activeVer.ChapterPageVersionId);
            if (dbVersion != null)
            {
                await FileResourceService.DeleteFileResourceAsync(dbVersion.PageFileId, _currentUserId);
                var fileRes = await FileResourceService.GetFileResourceByIdAsync(dbVersion.PageFileId);
                if (fileRes != null && !string.IsNullOrEmpty(fileRes.CloudinaryPublicId))
                {
                    await FileStorageService.DeleteFileAsync(fileRes.CloudinaryPublicId, "image");
                }
            }

            activeVer.IsDeleted = true;
            activeVer.DataUrl = "";
            Snackbar.Add($"Deleted v{activeVer.VersionNo} image.", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error deleting version: {ex.Message}", Severity.Error);
        }
        finally
        {
            IsProcessing = false;
            NotifyStateChanged();
        }
    }

    private async Task SetActiveVersion(string pane, PageVersionModel version)
    {
        if (IsChapterLocked) return;
        if (version.IsCurrentVersion) return;

        var page = pane == "Left" ? UploadedPages.ElementAtOrDefault(ActivePageIndex) : _splitUploadedPages.ElementAtOrDefault(_splitPageIndex);
        if (page == null) return;

        IsProcessing = true;
        NotifyStateChanged();
        Snackbar.Add("Setting active version...", Severity.Info);

        await _dbLock.WaitAsync();
        try
        {
            await VersionService.SetCurrentVersionAsync(page.ChapterPageId, version.ChapterPageVersionId);

            // Update UI state
            foreach (var v in page.Versions)
            {
                v.IsCurrentVersion = v.ChapterPageVersionId == version.ChapterPageVersionId;
            }

            Snackbar.Add("Active version updated successfully.", Severity.Success);
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            Snackbar.Add($"Failed to set active version: {msg}", Severity.Error);
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            IsProcessing = false;
            NotifyStateChanged();
            _dbLock.Release();
        }
    }

    private async Task SetTool(string tool)
    {
        CurrentTool = tool;
        await GetActiveCanvas()!.InvokeVoidAsync("setTool", tool);
    }

    private async Task ZoomIn() => await GetActiveCanvas()!.InvokeVoidAsync("zoom", 1.2);
    private async Task ZoomOut() => await GetActiveCanvas()!.InvokeVoidAsync("zoom", 0.8);


    private async Task DeleteSelectedRegions() => await GetActiveCanvas()!.InvokeVoidAsync("deleteSelectedRegions");

    private async Task Undo() => await GetActiveCanvas()!.InvokeVoidAsync("undo");
    private async Task Redo() => await GetActiveCanvas()!.InvokeVoidAsync("redo");

    private async Task RunSegmentAI()
    {
        IsProcessing = true;
        NotifyStateChanged();

        Snackbar.Add("Running Segmentation...", Severity.Info);
        var successSegment = await GetActiveCanvas()!.InvokeAsync<bool>("callSegmentAPI");

        IsProcessing = false;
        NotifyStateChanged();

        if (successSegment)
        {
            Snackbar.Add("Segmentation complete.", Severity.Success);
        }
        else
        {
            Snackbar.Add("Segmentation failed.", Severity.Error);
        }
    }

    private async Task RunTranslateAI(string targetLang = "vi")
    {
        var lang = targetLang == "en" ? "en" : "vi";
        IsProcessing = true;
        NotifyStateChanged();

        Snackbar.Add(lang == "en" ? "Running translation (English)..." : "Running translation (Vietnamese)...", Severity.Info);
        var translateResult = await GetActiveCanvas()!.InvokeAsync<string>("callTranslateAPI", lang);

        IsProcessing = false;
        NotifyStateChanged();

        if (translateResult == "success")
        {
            Snackbar.Add("Translation complete.", Severity.Success);
            if (UploadedPages.Any())
            {
                // Do not overwrite existing version DataUrl; translation overlay remains on canvas until explicitly saved as a new version
            }
        }
        else
        {
            Snackbar.Add($"Translation failed: {translateResult}", Severity.Error);
        }
    }

    [JSInvokable]
    public void OnRegionsUpdated(string pane, string newRegionsStr, bool canUndo, bool canRedo)
    {
        CanUndo = canUndo;
        CanRedo = canRedo;

        var page = pane == "Left" ? UploadedPages.ElementAtOrDefault(ActivePageIndex) : _splitUploadedPages.ElementAtOrDefault(_splitPageIndex);
        bool contentChanged = false;
        if (page != null && page.Versions.Any())
        {
            var activeVer = page.Versions[page.ActiveVersionIndex];
            // Ignore selection-only differences so that merely clicking a panel
            // (or a programmatic reload echo) is not treated as an unsaved edit.
            if (StripSelected(activeVer.Regions) != StripSelected(newRegionsStr))
            {
                activeVer.Regions = newRegionsStr;
                activeVer.IsDirty = true;
                contentChanged = true;
            }
        }

        if (pane == _activePane)
        {
            SelectedRegions.Clear();
            if (!string.IsNullOrEmpty(newRegionsStr))
            {
                try
                {
                    var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var allRegions = System.Text.Json.JsonSerializer.Deserialize<List<RegionModel>>(newRegionsStr, options);
                    if (allRegions != null)
                    {
                        SelectedRegions = allRegions.Where(r => r.Selected).ToList();
                        Console.WriteLine($"OnRegionsUpdated ({pane}): Found {SelectedRegions.Count} selected regions.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deserializing regions: {ex.Message}");
                }
            }
        }
        NotifyStateChanged();
        // Persist a crash-recovery shadow draft only on real content edits
        // (auto-segment, translate, draw/move/resize/delete, notes, restore).
        if (page != null && contentChanged)
        {
            _ = JS.InvokeVoidAsync("saveMmsDraft", page.ChapterPageId.ToString(), newRegionsStr);
            _ = ScheduleAutoSave();
        }
    }

    public async Task<string> SegmentImageJS(string pane, string base64Image)
    {
        try
        {
            var parts = base64Image.Split(',');
            var base64Data = parts.Length > 1 ? parts[1] : parts[0];
            var mimeType = "image/png";
            if (parts.Length > 1 && parts[0].StartsWith("data:"))
            {
                mimeType = parts[0].Substring(5, parts[0].IndexOf(';') - 5);
            }

            var imageBytes = Convert.FromBase64String(base64Data);
            var result = await AiService.SegmentImageAsync(imageBytes, "image.png", mimeType);

            if (result != null)
            {
                return JsonSerializer.Serialize(result);
            }
            return JsonSerializer.Serialize(new { status = "error", message = "AI returned null" });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { status = "error", message = ex.Message });
        }
    }

    [JSInvokable]
    public async Task<string> TranslateRegionsJS(string pane, string jsonRequest)
    {
        try
        {
            var request = JsonSerializer.Deserialize<MangaManagementSystem.Application.DTOs.AI.TranslateRequestDto>(jsonRequest);
            if (request == null) return JsonSerializer.Serialize(new { status = "error", message = "Invalid request format" });

            var result = await AiService.TranslateRegionsAsync(request);
            if (result != null)
            {
                return JsonSerializer.Serialize(result);
            }
            return JsonSerializer.Serialize(new { status = "error", message = "AI returned null" });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { status = "error", message = ex.Message });
        }
    }

    private async Task ApproveAndLockChapter(Guid chapterId)
    {
        var chapter = Chapters.FirstOrDefault(c => c.ChapterId == chapterId);
        if (chapter != null)
        {
            try
            {
                await ChapterService.UpdateChapterStatusAsync(chapterId, "APPROVED");
                chapter.StatusCode = "APPROVED";
                chapter.IsCompleted = true;
                NotifyStateChanged();
                Snackbar.Add("Chapter approved & locked successfully.", Severity.Success);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Failed to approve chapter: {ex.Message}", Severity.Error);
            }
        }
    }

    private async Task UnlockChapter(Guid chapterId)
    {
        var chapter = Chapters.FirstOrDefault(c => c.ChapterId == chapterId);
        if (chapter != null)
        {
            await _dbLock.WaitAsync();
            try
            {
                await ChapterService.UpdateChapterStatusAsync(chapterId, "DRAFT");
                chapter.StatusCode = "DRAFT";
                chapter.IsCompleted = false;
                NotifyStateChanged();
                Snackbar.Add("Chapter unlocked. Status back to Draft.", Severity.Info);
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Failed to unlock chapter: {ex.Message}", Severity.Error);
            }
            finally
            {
                _dbLock.Release();
            }
        }
    }


    private async void PerformAutoSave(object? state)
    {
        if (!await _autoSaveLock.WaitAsync(0)) return;
        try
        {
            await InvokeAsync(async () =>
            {
                try
                {
                    _isAutoSaving = true;
                    NotifyStateChanged();
                    await SaveProgress();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Auto-save error: {ex.Message}");
                }
                finally
                {
                    _isAutoSaving = false;
                    NotifyStateChanged();
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Auto-save InvokeAsync error: {ex.Message}");
        }
        finally
        {
            _autoSaveLock.Release();
        }
    }

    private void TryCancelPending()
    {
        _operationCts?.Cancel();
        _operationCts?.Dispose();
        _operationCts = new CancellationTokenSource();
    }

    private async Task ScheduleAutoSave()
    {
        if (_autoSaveTimer == null)
        {
            _autoSaveTimer = new System.Threading.Timer(PerformAutoSave, null, 30000, Timeout.Infinite);
        }
        else
        {
            _autoSaveTimer.Change(30000, Timeout.Infinite);
        }
        await Task.CompletedTask;
    }

    private async Task ToggleSplitView()
    {
        _isSplitView = !_isSplitView;
        if (_isSplitView)
        {
            _splitUploadedPages = UploadedPages.ToList();
            int targetRight = ActivePageIndex + 1;
            NotifyStateChanged();
            await Task.Delay(50); // wait for display: flex to apply so canvas container has valid width/height
            if (targetRight < _splitUploadedPages.Count)
            {
                await OnSplitPageChanged(targetRight + 1); // parameter is 1-indexed
            }
            else
            {
                await OnSplitPageChanged(ActivePageIndex + 1);
            }
        }
        else
        {
            _splitUploadedPages = new();
            NotifyStateChanged();
        }
    }

    private void SplitPagePrev()
    {
        if (_splitPageIndex > 0)
        {
            _splitPageIndex--;
            NotifyStateChanged();
        }
    }

    private void SplitPageNext()
    {
        if (_splitPageIndex < _splitUploadedPages.Count - 1)
        {
            _splitPageIndex++;
            NotifyStateChanged();
        }
    }

    public void Dispose()
    {
        _autoSaveTimer?.Dispose();
        _autoSaveLock?.Dispose();
        _uploadLock?.Dispose();
        _dbLock?.Dispose();
        _operationCts?.Cancel();
        _operationCts?.Dispose();
        _objRef?.Dispose();
    }

    public class RegionModel
    {
        public int Id { get; set; }
        public Guid? DbId { get; set; }
        public string? Label { get; set; }
        public string Type { get; set; } = "panel";
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public bool Selected { get; set; }
        public string? OriginalText { get; set; }
        public string? TranslatedText { get; set; }
    }
}
