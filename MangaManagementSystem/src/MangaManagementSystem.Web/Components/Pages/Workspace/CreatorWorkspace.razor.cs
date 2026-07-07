using static MangaManagementSystem.Web.Components.Pages.Workspace.WorkspaceHelpers;

namespace MangaManagementSystem.Web.Components.Pages.Workspace
{
    public partial class CreatorWorkspace
    {
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

    // Versions panel collapse state (default collapsed to save canvas space)
    private bool _versionsCollapsedLeft = true;
    private bool _versionsCollapsedRight = true;

    // Collapse the NEW TASK form to give the ACTIVE TASKS list more room (laptop screens)
    private bool _newTaskFormCollapsed = false;

    // Collapse the NEW ANNOTATION form to give the PAGE ISSUES list more room
    private bool _newAnnotationFormCollapsed = false;

    private SemaphoreSlim _dbSemaphore = new SemaphoreSlim(1, 1);

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
    private bool _isLeftPanelOpen = true;   // Chapters sidebar collapse (mirror of the task panel)

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
    private decimal TaskCompensation { get; set; } = 0m;   // manga.ChapterPageTask.compensation_amount (metadata; no currency unit per BR)
    private DateTime? _taskDueDate = DateTime.Today.AddDays(7);   // manga.ChapterPageTask.due_at_utc (deadline); null → service defaults to +7 days
    
    private void OnTaskTypeChanged(string value) => TaskType = value;
    private void OnAssistantChanged(Guid? value) => AssignedAssistantId = value;

    // Task logic
    private List<ProductionTask> ActiveTasks = new();

    // Task Panel: keep the target label compact — show the first couple of panels, then a "…(+N)"
    // tail. The full list stays visible via the tooltip on hover.
    private static string CompactTarget(string? target, int max = 2)
    {
        if (string.IsNullOrWhiteSpace(target)) return "";
        var parts = target.Split(", ", StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length <= max) return target;
        return string.Join(", ", parts.Take(max)) + $" …(+{parts.Length - max})";
    }

    // --- Version-scoped task/annotation display (Option B) -------------------------------
    // Tasks and annotations belong to the specific version of their regions (BR-CP-015/016),
    // so the Task Panel and canvas pins show only items of the currently active version.
    private Guid GetActiveVersionId()
    {
        var page = UploadedPages.ElementAtOrDefault(ActivePageIndex);
        if (page == null || page.Versions.Count == 0) return Guid.Empty;
        var idx = Math.Clamp(page.ActiveVersionIndex, 0, page.Versions.Count - 1);
        return page.Versions[idx].ChapterPageVersionId;
    }

    private bool MatchesActiveVersion(Guid? versionId)
    {
        // null = no linked region yet (freshly created/local) → show on the current version.
        if (versionId == null) return true;
        return versionId.Value == GetActiveVersionId();
    }

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
                    PageRegionId: region.DbId,
                    CreatedByUserId: _currentUserId
                );

                var dbRegion = await MangakaRegionApi.CreateAsync(_currentUserId ?? Guid.Empty, newRegionDto);
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
                foreach(var r in allRegions)
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

        // compensation_amount is DECIMAL(12,2) >= 0. Validate up front so a bad value gives a friendly
        // message instead of a raw SQL CHECK/overflow error (the field also clamps via Min/Max).
        if (TaskCompensation < 0)
        {
            Snackbar.Add("Compensation cannot be negative.", Severity.Warning);
            return;
        }
        if (TaskCompensation > 9_999_999_999.99m)
        {
            Snackbar.Add("Compensation is too large (max 9,999,999,999.99).", Severity.Warning);
            return;
        }

        // Deadline (manga.ChapterPageTask.due_at_utc) must not be in the past when the user sets one.
        if (_taskDueDate.HasValue && _taskDueDate.Value.Date < DateTime.Today)
        {
            Snackbar.Add("Deadline cannot be in the past.", Severity.Warning);
            return;
        }

        // A task references PageRegion(s) of a saved page version (BR-PGTASK-001/007), so the page
        // must be persisted first. Prompt to Save if it is still in the buffer.
        if (!await EnsureSavedBeforeAsync()) return;

        var regionsToSave = SelectedRegions.ToList();

        List<Guid> regionIds;
        string target;
        if (!regionsToSave.Any())
        {
            // #8 / BR-REG-031: with no explicit panel selection, anchor the task to the page's
            // whole-page (FULL_PAGE) region — found-or-created server-side from Cloudinary dimensions —
            // instead of refusing. Restores the full-page default without fabricating phantom (0,0) pins.
            var fullPageVersionId = GetActiveVersionId();
            if (fullPageVersionId == Guid.Empty)
            {
                Snackbar.Add("Please open a saved page version first.", Severity.Warning);
                return;
            }
            try
            {
                var fullPage = await MangakaRegionApi.EnsureFullPageRegionAsync(_currentUserId.Value, fullPageVersionId);
                regionIds = new List<Guid> { fullPage.PageRegionId };
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Could not prepare a full-page target: {ex.Message}", Severity.Error);
                return;
            }
            target = "Full page";
        }
        else
        {
            regionIds = await EnsureRegionsSavedAsync(regionsToSave);
            // Show only the panel number(s) — same #N shown on the canvas — not raw coordinates.
            target = string.Join(", ", SelectedRegions.Select(r => $"Panel #{r.Id}"));
        }

        int newId = ActiveTasks.Any() ? ActiveTasks.Max(t => t.Id) + 1 : 1;

        try
        {
            // Architecture: create the task via the typed API client (→ controller → service → SP).
            var dbTask = await MangakaTaskApi.CreateTaskAsync(_currentUserId.Value, new CreateMangakaTaskRequest(
                AssignedToUserId: AssignedAssistantId.Value,
                TypeCode: TaskType,
                TaskTitle: $"{TaskType} Task for {target}",
                TaskDescription: TaskDescription,
                PriorityLevel: 2,
                CompensationAmount: Math.Round(TaskCompensation, 2, MidpointRounding.AwayFromZero),
                PageRegionIds: regionIds,
                DueAtUtc: _taskDueDate.HasValue
                    ? DateTime.SpecifyKind(_taskDueDate.Value.Date, DateTimeKind.Utc).AddHours(23).AddMinutes(59)
                    : (DateTime?)null));

            var assistantName = _assistantUsers.FirstOrDefault(u => u.UserId == AssignedAssistantId.Value)?.Username ?? AssignedAssistantId.ToString();

            ActiveTasks.Insert(0, new ProductionTask 
            { 
                Id = newId, 
                DbId = dbTask.ChapterPageTaskId,
                DueAtUtc = dbTask.DueAtUtc,
                Type = TaskType,
                Assistant = assistantName,
                Target = target,
                Description = TaskDescription,
                Status = "Assigned",
                VersionId = GetActiveVersionId(),
                Regions = regionsToSave.ToList()   // so clicking the new card highlights its panels immediately
            });
            TaskDescription = ""; // Reset form
            TaskCompensation = 0m;
            _taskDueDate = DateTime.Today.AddDays(7);
            Snackbar.Add("Task assigned successfully!", Severity.Success);
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error creating task: {ex.Message}", Severity.Error);
        }
    }
    
    // CycleTaskStatus removed: it flipped a cosmetic "Todo"/"In Progress" label that both mapped to
    // ASSIGNED (a no-op DB write), contradicting BR-PGTASK-016 (no "started" tracking) and letting the
    // Mangaka pretend to set a status they do not own. Status is now read-only in the workspace;
    // transitions happen via their role-owned actions (assistant submit, Mangaka approve/return/cancel).

    // Cancel — never hard-delete. BR-PGTASK-015/027/029/031: a saved task is preserved and moved to
    // CANCELLED with a reason for traceability + audit. The reason is collected by the dialog below
    // and recorded by the SP (usp_ChapterPageTask_Cancel), which also blocks cancelling a task that
    // is already COMPLETED/CANCELLED.
    private bool _showCancelTaskDialog;
    private int _cancelTaskLocalId;
    private string _cancelTaskReason = string.Empty;
    private bool _cancelTaskInProgress;

    private void OpenCancelTaskDialog(int taskId)
    {
        if (IsChapterLocked) return;
        _cancelTaskLocalId = taskId;
        _cancelTaskReason = string.Empty;
        _showCancelTaskDialog = true;
    }

    private async Task ConfirmCancelTask()
    {
        if (string.IsNullOrWhiteSpace(_cancelTaskReason)) return;   // SP requires a non-empty reason
        var task = ActiveTasks.FirstOrDefault(t => t.Id == _cancelTaskLocalId);
        if (task == null) { _showCancelTaskDialog = false; return; }

        _cancelTaskInProgress = true;
        try
        {
            if (task.DbId.HasValue && _currentUserId.HasValue)
            {
                // Architecture: cancel via the typed API client (→ controller → MediatR/service → SP).
                await MangakaTaskApi.CancelTaskAsync(_currentUserId.Value, task.DbId.Value, _cancelTaskReason.Trim());
                task.Status = "Cancelled";   // kept in the list (greyed out), not removed — for traceability
            }
            else
            {
                // No DbId means it was never persisted — safe to drop from the working set.
                ActiveTasks.Remove(task);
            }
            _showCancelTaskDialog = false;
            Snackbar.Add("Task cancelled.", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to cancel task: {ex.InnerException?.Message ?? ex.Message}", Severity.Error);
        }
        finally
        {
            _cancelTaskInProgress = false;
            StateHasChanged();
        }
    }

    // Annotation logic
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
        StateHasChanged();
    }

    [JSInvokable]
    public void OnToolChangedFromJS(string pane, string tool)
    {
        CurrentTool = tool;
        StateHasChanged();
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
                var currentAnnotations = ActiveAnnotations.Where(a => a.PageNumber == SelectedPage && MatchesActiveVersion(a.VersionId) && a.PinX.HasValue && a.PinY.HasValue);
                await canvas.InvokeVoidAsync("syncAnnotations", currentAnnotations.Select(a => new { pinX = a.PinX.Value, pinY = a.PinY.Value, isResolved = a.IsResolved }));
            }
            else
            {
                await canvas.InvokeVoidAsync("syncAnnotations", Array.Empty<object>());
            }
        }
    }

    // Saves manga.ChapterPage.page_notes (whole-page note, shared across versions). Explicit button,
    // immediate write — same pattern as the version note. The page must be persisted first.
    private async Task SavePageNote()
    {
        if (!UploadedPages.Any()) return;
        if (!await EnsureSavedBeforeAsync()) return;
        var activePage = UploadedPages[ActivePageIndex];
        if (activePage.ChapterPageId == Guid.Empty || !_currentUserId.HasValue) return;

        try
        {
            // Architecture: update the page note via the typed API client (the server keeps chapter/page_no).
            await MangakaPageApi.UpdateNotesAsync(_currentUserId.Value, activePage.ChapterPageId, activePage.PageNotes);
            Snackbar.Add("Page note saved.", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error saving page note: {ex.Message}", Severity.Error);
        }
    }

    private async Task SaveVersionNote()
    {
        if (!UploadedPages.Any()) return;
        // The note is stored on the saved ChapterPageVersion, so the page must be persisted first.
        if (!await EnsureSavedBeforeAsync()) return;
        var activePage = UploadedPages[ActivePageIndex];
        if (!activePage.Versions.Any()) return;
        var activeVer = activePage.Versions[activePage.ActiveVersionIndex];

        try
        {
            var dbVer = await MangakaPageApi.GetVersionByIdAsync(_currentUserId!.Value, activeVer.ChapterPageVersionId);
            if (dbVer != null)
            {
                await MangakaPageApi.UpdateVersionAsync(_currentUserId!.Value, new UpdateChapterPageVersionDto(
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

        // An annotation is anchored to PageRegion(s) of a saved page version (BR-ANN-001/004), so
        // the page must be persisted first. Prompt to Save if it is still in the buffer.
        if (!await EnsureSavedBeforeAsync()) return;

        var regionsToSave = SelectedRegions.ToList();
        List<Guid> regionIds;
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
            regionIds = await EnsureRegionsSavedAsync(regionsToSave);
        }
        else if (!regionsToSave.Any())
        {
            // #8 / BR-REG-031: no panel selected and no pin placed → anchor the annotation to the
            // page's whole-page (FULL_PAGE) region (found-or-created server-side) instead of refusing.
            var fullPageVersionId = GetActiveVersionId();
            if (fullPageVersionId == Guid.Empty)
            {
                Snackbar.Add("Please open a saved page version first.", Severity.Warning);
                return;
            }
            try
            {
                var fullPage = await MangakaRegionApi.EnsureFullPageRegionAsync(_currentUserId.Value, fullPageVersionId);
                regionIds = new List<Guid> { fullPage.PageRegionId };
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Could not prepare a full-page target: {ex.Message}", Severity.Error);
                return;
            }
        }
        else
        {
            regionIds = await EnsureRegionsSavedAsync(regionsToSave);
        }

        int newId = ActiveAnnotations.Any() ? ActiveAnnotations.Max(t => t.Id) + 1 : 1;
        
        // Show only the panel number(s) — same #N shown on the canvas — not raw coordinates.
        string target;
        if (PendingPinX.HasValue && PendingPinY.HasValue)
            target = $"Pin at ({Math.Round(PendingPinX.Value)}, {Math.Round(PendingPinY.Value)})";
        else if (!SelectedRegions.Any())
            target = "Full page";
        else
            target = string.Join(", ", SelectedRegions.Select(r => $"Panel #{r.Id}"));

        try
        {
            // Architecture: create the annotation via the typed API client (author from actor header).
            var dbAnn = await MangakaAnnotationApi.CreateAsync(_currentUserId.Value, new CreateMangakaAnnotationRequest(
                IssueTypeCode: AnnotationType,
                AnnotationText: AnnotationComment,
                PageRegionIds: regionIds));

            ActiveAnnotations.Insert(0, new AnnotationModel 
            { 
                Id = newId, 
                DbId = dbAnn.ChapterPageAnnotationId,
                Type = AnnotationType,
                Comment = AnnotationComment,
                Target = target,
                PageNumber = SelectedPage,
                PinX = PendingPinX,
                PinY = PendingPinY,
                VersionId = GetActiveVersionId(),
                Regions = regionsToSave.ToList()   // so clicking the new card highlights its panels immediately
            });
            AnnotationComment = "";
            PendingPinX = null;
            PendingPinY = null;
            Snackbar.Add("Annotation added successfully!", Severity.Success);
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error creating annotation: {ex.Message}", Severity.Error);
        }
        if (GetActiveCanvas() != null)
        {
            var pageAnnotations = ActiveAnnotations.Where(a => (a.PageNumber == SelectedPage || a.PageNumber == 0) && MatchesActiveVersion(a.VersionId)).ToList();
            await GetActiveCanvas()!.InvokeVoidAsync("syncAnnotations", pageAnnotations);
        }
    }

    private async Task SyncAnnotationsToJS()
    {
        if (GetActiveCanvas() != null)
        {
            var pageAnnotations = ActiveAnnotations.Where(a => (a.PageNumber == SelectedPage || a.PageNumber == 0) && MatchesActiveVersion(a.VersionId)).ToList();
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
                    await MangakaAnnotationApi.ResolveAsync(_currentUserId.Value, ann.DbId.Value);
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
            StateHasChanged();
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
                // BR-ANN-017 / BR-ANN-024: a saved annotation is preserved for traceability and must
                // not be deleted — it is closed by resolving it. Only a local draft annotation that
                // was never persisted may be discarded from the working set.
                if (ann.DbId.HasValue)
                {
                    Snackbar.Add("Saved annotations cannot be deleted. Resolve it instead to keep the feedback history.", Severity.Warning);
                    return;
                }
                ActiveAnnotations.Remove(ann);
                await SyncAnnotationsToJS();
                StateHasChanged();
            }
        }
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

        // Fetch assistant users for the dropdown from Series Contributors
        try
        {
            if (Guid.TryParse(SeriesId, out var sId) && _currentUserId.HasValue)
            {
                var contributors = await MangakaContributorApi.GetContributorsAsync(_currentUserId.Value, sId);
                var assistantList = new List<Application.DTOs.Auth.UserDto>();
                foreach (var c in contributors.Where(c => c.IsActive && string.Equals(c.RoleName, "Assistant", StringComparison.OrdinalIgnoreCase)))
                {
                    if (assistantList.All(a => a.UserId != c.UserId))
                    {
                        assistantList.Add(new Application.DTOs.Auth.UserDto(c.UserId, c.Username ?? "", c.Username ?? "", c.Email ?? "", null, null, "ACTIVE", DateTime.UtcNow, c.RoleName));
                    }
                }
                _assistantUsers = assistantList;
            }
        }
        catch (Exception)
        {
            _assistantUsers = new List<Application.DTOs.Auth.UserDto>();
        }

        if (!string.IsNullOrWhiteSpace(SeriesId) && Guid.TryParse(SeriesId, out Guid id))
        {
            // Architecture: series header info via the typed API client (by slug), not SeriesService.
            var series = await SeriesApiClient.GetSeriesDetailAsync(Slug);
            if (series != null)
            {
                SeriesTitle = series.Title;
                SeriesSubtitle = string.Join(", ", series.Genres.Select(g => g.GenreName));
                
                var chapters = await MangakaChapterApi.GetSeriesChaptersAsync(_currentUserId!.Value, id);
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
                    
                    var pageCountsDict = await MangakaPageApi.GetCountsAsync(_currentUserId!.Value, chapterModels.Select(c => c.ChapterId).ToList());
                    foreach(var cm in chapterModels)
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
                                var task = await MangakaTaskApi.GetTaskDetailAsync(_currentUserId!.Value, parsedTaskId);
                                if (task != null)
                                {
                                    resolveChapterId = task.ChapterId;
                                    _taskTargetVersionId = task.SourceChapterPageVersionId;
                                    _taskFilterId = taskId;
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
            }
        }
        _seriesNotFound = true;
    }

    private async Task SelectChapter(int chapterId)
    {
        if (_isLoadingChapter) return;
        
        SelectedChapter = chapterId;
        try {
        ActivePageIndex = 0;
        SelectedPage = 0; // Reset selected page to avoid UI crashes
        
        var chapter = Chapters.FirstOrDefault(c => c.Id == chapterId);
        if (chapter != null && !chapter.IsPagesLoaded)
        {
            await _dbSemaphore.WaitAsync();
            try
            {
                _isLoadingChapter = true;
                StateHasChanged();
                var pages = (await MangakaPageApi.GetByChapterAsync(_currentUserId!.Value, chapter.ChapterId)).ToList();
                var pageIds = pages.Select(p => p.ChapterPageId).Distinct().ToList();
                var allVersions = (await MangakaPageApi.GetVersionsByPageIdsAsync(_currentUserId!.Value, pageIds)).ToList();
                
                var fileIds = allVersions.Select(v => v.PageFileId).Where(fid => fid != Guid.Empty).Distinct().ToList();
                var filesDict = (await MangakaPageApi.GetFileResourcesByIdsAsync(_currentUserId!.Value, fileIds)).ToDictionary(f => f.FileResourceId);
                
                var verIds = allVersions.Select(v => v.ChapterPageVersionId).Distinct().ToList();

                // Guard against pathological region counts (corrupt / exploded segmentation
                // data, e.g. a page with hundreds of thousands of regions). Only materialize
                // regions for versions within a sane cap so one bad page cannot freeze the
                // entire chapter load. Over-cap versions load with no regions but remain
                // viewable and deletable.
                const int MaxRegionsPerVersion = 2000;
                var regionCounts = await MangakaRegionApi.GetCountsAsync(_currentUserId ?? Guid.Empty, verIds);
                var safeVerIds = verIds
                    .Where(vid => !regionCounts.TryGetValue(vid, out var cnt) || cnt <= MaxRegionsPerVersion)
                    .ToList();
                bool anyVersionCapped = safeVerIds.Count < verIds.Count;
                var regionsGrouped = (await MangakaRegionApi.GetByVersionsAsync(_currentUserId ?? Guid.Empty, safeVerIds)).ToLookup(r => r.ChapterPageVersionId);
                
                var versionsGrouped = allVersions.ToLookup(v => v.ChapterPageId);

                // #7: replace, never append. This load also runs on a reload (Discard resets
                // IsPagesLoaded), so clearing first prevents the DB pages from stacking on top of the
                // existing in-memory list — which duplicated every page before the discarded one.
                chapter.Pages.Clear();

                foreach(var p in pages)
                {
                    var pageModel = new PageModel { ChapterPageId = p.ChapterPageId, PageNotes = p.PageNotes };
                    var versions = versionsGrouped[p.ChapterPageId];
                    foreach(var v in versions)
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

                              mappedRegions.Add(new RegionModel {
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

                          var camelOptions = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };
                          string regionsJson = mappedRegions.Any() 
                              ? System.Text.Json.JsonSerializer.Serialize(mappedRegions, camelOptions) 
                              : "[]";

                          pageModel.Versions.Add(new PageVersionModel {
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
                    Snackbar.Add("Some pages have an abnormal number of regions and were skipped while loading to avoid freezing. Delete those pages to clean up the corrupt data.", Severity.Warning);
                }
            }
            finally
            {
                _isLoadingChapter = false;
                _dbSemaphore.Release();
                StateHasChanged();
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
            // Chapter genuinely has no pages (the list has finished loading): clear the canvas and
            // prompt to upload. Reliable here, unlike the racy first-render check.
            if (_leftCanvasRef != null)
                await _leftCanvasRef.InvokeVoidAsync("loadImage", "");
            if (_rightCanvasRef != null)
                await _rightCanvasRef.InvokeVoidAsync("loadImage", "");
            Snackbar.Add("Please upload an image to begin.", Severity.Info);
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
        StateHasChanged();
        } catch (TaskCanceledException) {
            // Ignore cancelled tasks during re-rendering or fast switching
        } catch (Exception ex) {
            Snackbar.Add($"Error loading chapter: {ex.Message}", Severity.Error);
            Console.WriteLine(ex.ToString());
        }
    }

    private async Task SubmitChapterForReview(Guid chapterId)
    {
        // Resolve by id, or — when the chapter is still pending (Guid.Empty) — the selected one.
        var chapter = (chapterId != Guid.Empty ? Chapters.FirstOrDefault(c => c.ChapterId == chapterId) : null)
                      ?? Chapters.FirstOrDefault(c => c.Id == SelectedChapter);
        if (chapter == null) return;

        // Confirm before submitting — submission locks the chapter for editing while the editor reviews.
        bool? confirmSubmit = await DialogService.ShowMessageBox(
            "Submit for Review",
            "Are you sure you want to submit this chapter for review? Once submitted, the chapter is locked for editing until the editor completes the review.",
            yesText: "Submit", cancelText: "Cancel");
        if (confirmSubmit != true) return;

        // Submitting needs the chapter and its pages saved to the DB first.
        if (!await EnsureSavedBeforeAsync("chapter")) return;
        if (chapter.ChapterId == Guid.Empty) return;   // safety: save did not produce an id

        if (!_currentUserId.HasValue) return;

        try
        {
            // Architecture: route through the typed API client (Razor → API client → controller →
            // MediatR → handler → SP), not a direct Application service call.
            var result = await MangakaChapterApi.SubmitChapterForReviewAsync(_currentUserId.Value, chapter.ChapterId);
            chapter.StatusCode = result.StatusCode;   // authoritative status from the server (UNDER_REVIEW)
            chapter.IsCompleted = false;
            StateHasChanged();
            Snackbar.Add("Chapter submitted for review.", Severity.Success);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to submit: {ex.Message}", Severity.Error);
        }
    }

    private async Task CancelSubmission(Guid chapterId)
    {
        var chapter = Chapters.FirstOrDefault(c => c.ChapterId == chapterId);
        if (chapter == null || !_currentUserId.HasValue) return;

        try
        {
            // Architecture: route through the typed API client (→ controller → MediatR → handler → EF).
            var result = await MangakaChapterApi.CancelChapterSubmissionAsync(_currentUserId.Value, chapterId);
            chapter.StatusCode = result.StatusCode;   // authoritative status from the server (DRAFT)
            StateHasChanged();
            Snackbar.Add("Submission cancelled. Chapter is now back to DRAFT.", Severity.Info);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to cancel submission: {ex.Message}", Severity.Error);
        }
    }

    private async Task AddNewChapter()
    {
        if (_isAddingChapter) return;
        
        try
        {
            _isAddingChapter = true;
            StateHasChanged();
            
            if (Guid.TryParse(SeriesId, out Guid seriesGuid))
            {
                // Chapter numbers must be unique across ALL chapters in the series,
                // including CANCELLED ones: uq_chapter_series_chapter_number is NOT a
                // filtered index, and the workspace hides cancelled chapters. Computing
                // the next number from the visible list alone collided with a hidden
                // cancelled chapter ("duplicate key (series, 1)"). Use the full list.
                // Read-only number probe (no DB write): pick a display number that does not collide
                // with existing chapters, including hidden CANCELLED ones. The authoritative
                // chapter_number_label is re-checked at Save time in FlushPendingAsync.
                var allChapters = await MangakaChapterApi.GetSeriesChaptersAsync(_currentUserId!.Value, seriesGuid);
                int maxExisting = allChapters
                    .Select(c => int.TryParse(c.ChapterNumberLabel, out var n) ? n : 0)
                    .DefaultIfEmpty(0)
                    .Max();
                int newId = Math.Max(maxExisting, Chapters.Any() ? Chapters.Max(c => c.Id) : 0) + 1;

                // MANUAL-SAVE: create the chapter in the in-memory buffer only (ChapterId stays
                // Guid.Empty). It is persisted by SaveAllChangesAsync → FlushPendingAsync when the
                // user clicks Save, so throwaway "test" chapters never hit the DB. IsPagesLoaded=true
                // keeps SelectChapter from trying to load pages for an id that does not exist yet.
                Chapters.Add(new ChapterModel { Id = newId, ChapterId = Guid.Empty, PageCount = 0, IsCompleted = false, Title = "", IsPagesLoaded = true });
                await SelectChapter(newId);
                _saveState = SaveStatus.Dirty;
                _ = JS.InvokeVoidAsync("setUnsavedFlag", true);
                Snackbar.Add($"Chapter {newId} added (unsaved). Click Save to persist.", Severity.Info);
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
            StateHasChanged();
        }
    }

    private async Task DeleteChapter(int chapterId)
    {
        var chapter = Chapters.FirstOrDefault(c => c.Id == chapterId);
        if (chapter == null) return;

        // #1: a never-saved (pending) chapter exists only in the UI. Discard = drop it from the list;
        // there is no DB row to cancel, so we must NOT call the backend (that caused the "Invalid
        // chapter ID" error) and must NOT mark it CANCELLED (a state only saved chapters can have).
        if (chapter.ChapterId == Guid.Empty)
        {
            bool? discard = await DialogService.ShowMessageBox(
                "Discard Chapter",
                "Discard this unsaved chapter? It has not been saved yet, so nothing will be kept.",
                yesText: "Discard", cancelText: "Keep");
            if (discard != true) return;

            Chapters.Remove(chapter);

            // If it was the open chapter, move to another chapter (or clear the workspace).
            if (SelectedChapter == chapter.Id)
            {
                var next = Chapters.FirstOrDefault();
                if (next != null)
                {
                    await SelectChapter(next.Id);
                }
                else
                {
                    SelectedChapter = 0;
                    SelectedPage = 0;
                    _saveState = SaveStatus.Saved;
                    _imageDirty = false;
                    _ = JS.InvokeVoidAsync("setUnsavedFlag", false);
                }
            }

            Snackbar.Add("Unsaved chapter discarded.", Severity.Info);
            StateHasChanged();
            return;
        }

        bool? result = await DialogService.ShowMessageBox(
            "Cancel Chapter",
            "Are you sure you want to cancel this chapter? It will be kept for history but marked CANCELLED (its pages, versions and feedback are preserved).",
            yesText: "Cancel Chapter", cancelText: "Keep");

        if (result != true) return;

        if (!_currentUserId.HasValue) return;
        try
        {
            // Architecture: cancel via the typed API client (→ controller → MediatR → handler → EF,
            // which sets CANCELLED + writes the CHAPTER_CANCELLED audit event). Content preserved.
            var dto = await MangakaChapterApi.CancelChapterAsync(_currentUserId.Value, chapter.ChapterId);
            chapter.StatusCode = dto.StatusCode;   // authoritative (CANCELLED)
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to cancel chapter: {ex.Message}", Severity.Error);
            return;
        }

        Snackbar.Add($"Chapter {chapter.Id} cancelled.", Severity.Success);
        StateHasChanged();
    }

    private void PromptRenameChapter(ChapterModel chapter)
    {
        chapter.IsRenaming = true;
    }

    private async Task SaveChapterName(ChapterModel chapter)
    {
        if (chapter.ChapterId != Guid.Empty && _currentUserId.HasValue)
        {
            try
            {
                // Architecture: update the draft (title) via the typed API client. The request carries
                // the current number label (unchanged) + the new title.
                await MangakaChapterApi.UpdateChapterDraftAsync(
                    _currentUserId.Value,
                    chapter.ChapterId,
                    new UpdateChapterDraftRequest(
                        chapter.Id.ToString(),
                        string.IsNullOrWhiteSpace(chapter.Title) ? null : chapter.Title));
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
            StateHasChanged();
        }
    }
    
    private IJSObjectReference? GetActiveCanvas() => _activePane == "Left" ? _leftCanvasRef : _rightCanvasRef;

    // Clicking a task/annotation card highlights its panels on the page: select on the canvas the
    // regions whose PageRegionId matches the card's linked regions (pins are skipped — they are not
    // drawn as selectable boxes). Only active-version cards are shown, so their regions are on canvas.
    private async Task SelectPanelsByRegions(IEnumerable<RegionModel>? regions)
    {
        var canvas = GetActiveCanvas();
        if (canvas == null) return;

        var dbIds = (regions ?? Enumerable.Empty<RegionModel>())
            .Where(r => r.DbId.HasValue && !(r.Width <= 0.05 && r.Height <= 0.05))
            .Select(r => r.DbId!.Value.ToString())
            .ToArray();

        if (dbIds.Length == 0)
        {
            Snackbar.Add("No panel target to highlight on this page.", Severity.Info);
            return;
        }

        await canvas.InvokeVoidAsync("selectRegionsByDbIds", new object[] { dbIds });
    }

    private Task SelectTaskPanels(ProductionTask task) => SelectPanelsByRegions(task.Regions);
    private Task SelectAnnotationPanels(AnnotationModel ann) => SelectPanelsByRegions(ann.Regions);

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
        public void OnImageEdited() => _parent.OnImageEdited(Pane);
        
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

    // --- Chunk 4: autosave + save-state indicator ---------------------------------------
    private enum SaveStatus { Saved, Dirty, Saving }
    private SaveStatus _saveState = SaveStatus.Saved;
    private string? _saveProgress;   // #3: long-running save progress text (e.g. "Uploading pages… (2/5)")
    private DateTime? _lastSavedAtUtc;

    // Brush/clean paints PIXELS on the image, which autosave does not persist (only a new
    // Cloudinary upload via "Save as New Version" does). Track it separately so the indicator
    // can warn the user instead of falsely showing "Saved".
    private bool _imageDirty;
    private bool _imageEditHintShown;

    private bool HasUnsavedChanges => _saveState == SaveStatus.Dirty || _imageDirty;

    private async Task HandleBackClick()
    {
        // Manual-save model: if there are unsaved changes, ask before leaving (Phase 3 dialog).
        if (HasUnsavedChanges)
        {
            bool? result = await DialogService.ShowMessageBox(
                "Unsaved changes",
                "You have unsaved changes. Save before leaving?",
                yesText: "Save", noText: "Discard", cancelText: "Cancel");
            if (result == null) return;            // Cancel → stay
            if (result == true) await SaveAllChangesAsync();
        }
        Nav.NavigateTo(BackHref);
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
            
            // If a chapter's pages were already loaded by OnInitializedAsync before this first render,
            // paint the page now that the canvas exists. Do NOT show a "please upload" prompt here:
            // at first render the page list may still be loading (SelectChapter runs during init),
            // which previously surfaced a spurious prompt even for chapters that have pages. The
            // genuine "no pages" hint is shown by SelectChapter once the list has loaded.
            if (UploadedPages.Any())
            {
                await LoadPage(ActivePageIndex);
            }
        }
    }

    // Add-pages chooser + double-page split state.
    private bool _showUploadChoice;
    private bool _showDoublePageCrop;
    private string? _doublePageSourceDataUrl;
    private int _doublePageStep;   // 1 = right page, 2 = left page

    // "Split Double Page" picked one wide spread image → open the crop dialog for its first (right) page.
    private async Task HandleDoublePageSelected(InputFileChangeEventArgs e)
    {
        _showUploadChoice = false;
        if (IsChapterLocked) return;

        var activeChap = Chapters.FirstOrDefault(c => c.Id == SelectedChapter);
        if (activeChap == null)
        {
            Snackbar.Add("Please select a chapter first.", Severity.Warning);
            return;
        }

        var file = e.File;
        if (file == null) return;

        try
        {
            using var stream = file.OpenReadStream(maxAllowedSize: 1024 * 1024 * 20);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            _doublePageSourceDataUrl = $"data:{file.ContentType};base64,{Convert.ToBase64String(ms.ToArray())}";
            _doublePageStep = 1;
            _showDoublePageCrop = true;
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error reading image: {ex.InnerException?.Message ?? ex.Message}", Severity.Error);
        }
    }

    // Each confirmed crop becomes a PENDING page. After the first (right) page, the @key change
    // re-creates the crop dialog on the same source for the second (left) page.
    private async Task OnDoublePageCropConfirmed((byte[] Bytes, string FileName, string ContentType) result)
    {
        var activeChap = Chapters.FirstOrDefault(c => c.Id == SelectedChapter);
        if (activeChap == null) { _showDoublePageCrop = false; return; }

        AddPendingPage(activeChap, result.Bytes, result.FileName, result.ContentType);
        activeChap.PageCount = activeChap.Pages.Count;
        _saveState = SaveStatus.Dirty;
        _ = JS.InvokeVoidAsync("setUnsavedFlag", true);

        if (_doublePageStep == 1)
        {
            _doublePageStep = 2;   // re-create the crop dialog for the left page of the same spread
            StateHasChanged();
            return;
        }

        // Both pages added.
        _showDoublePageCrop = false;
        _doublePageSourceDataUrl = null;
        _doublePageStep = 0;
        StateHasChanged();
        await Task.Delay(1);
        await LoadPage(Math.Max(0, activeChap.Pages.Count - 2));
        Snackbar.Add("2 page(s) added from the spread (unsaved). Click Save to persist.", Severity.Info);
    }

    private void OnDoublePageCropCancelled()
    {
        // Pages confirmed before cancelling stay in the buffer (the user can delete them if unwanted).
        _showDoublePageCrop = false;
        _doublePageSourceDataUrl = null;
        _doublePageStep = 0;
    }

    // Adds one image to the chapter as a PENDING page (manual-save buffer): raw bytes held in memory
    // + a base64 data URL for the canvas. Nothing hits Cloudinary/DB until Save → FlushPendingAsync.
    private void AddPendingPage(ChapterModel chap, byte[] bytes, string fileName, string contentType)
    {
        var dataUrl = $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
        var newPage = new PageModel { ChapterPageId = Guid.Empty };
        newPage.Versions.Add(new PageVersionModel
        {
            VersionNo = 1,
            DataUrl = dataUrl,
            Note = "Original Upload",
            ChapterPageVersionId = Guid.Empty,
            IsCurrentVersion = true,
            IsDirty = false,
            PendingBytes = bytes,
            PendingFileName = fileName,
            PendingContentType = contentType
        });
        chap.Pages.Add(newPage);
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

            var files = e.GetMultipleFiles(100);
            if (files.Any())
            {
                var activeChap = Chapters.FirstOrDefault(c => c.Id == SelectedChapter);
                if (activeChap == null)
                {
                    Snackbar.Add("Please select a chapter first.", Severity.Warning);
                    return;
                }

                // #4: read every selected image into memory, build small preview thumbnails, then let
                // the user REVIEW + confirm before buffering. On Add the images are held in the
                // in-memory buffer (manual-save) — nothing hits Cloudinary/DB until the user clicks Save.
                var staged = new List<(byte[] Bytes, string Name, string ContentType, string DataUrl)>();
                foreach (var file in files)
                {
                    using var stream = file.OpenReadStream(maxAllowedSize: 1024 * 1024 * 20);
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream);
                    var bytes = memoryStream.ToArray();
                    var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "image/png" : file.ContentType;
                    staged.Add((bytes, file.Name, contentType, $"data:{contentType};base64,{Convert.ToBase64String(bytes)}"));
                }

                var thumbs = await BuildPreviewThumbnailsAsync(staged.Select(s => s.DataUrl));
                ShowUploadConfirm(
                    staged.Count == 1 ? "Add page" : $"Add {staged.Count} pages",
                    thumbs,
                    async () =>
                    {
                        int firstNewIndex = activeChap.Pages.Count;
                        foreach (var s in staged)
                        {
                            AddPendingPage(activeChap, s.Bytes, s.Name, s.ContentType);
                        }
                        activeChap.PageCount = activeChap.Pages.Count;
                        _saveState = SaveStatus.Dirty;
                        _ = JS.InvokeVoidAsync("setUnsavedFlag", true);
                        StateHasChanged();
                        await Task.Delay(1); // Yield to allow the pagination DOM to render if it was empty
                        await LoadPage(firstNewIndex);
                        Snackbar.Add($"Added {staged.Count} page(s) (unsaved). Click Save to upload & persist.", Severity.Info);
                    });
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

                }
            }
        }
        finally
        {
            _isPageLoading = false;
            // Re-render after clearing the guard. The initial load runs from
            // OnAfterRenderAsync (which does NOT auto-render afterwards), so without this
            // the UI kept rendering _isPageLoading = true and the pagination stayed
            // visually Disabled forever after a reload.
            StateHasChanged();
        }
    }

    private async Task OnPageChanged(int pageNumber)
    {
        // Ignore clicks while a load is still running so rapid pagination clicks
        // cannot pile up overlapping DB + JS calls on the Blazor Server circuit
        // (the cause of slow loads and dropped connections).
        if (_isPageLoading) return;
        // Manual-save model: do NOT persist on navigation. Unsaved region edits stay in the
        // in-memory buffer (PageVersionModel.Regions) and are flushed only by the Save button.
        // MudPagination pageNumber is 1-indexed
        await LoadPage(pageNumber - 1);
    }

    // Re-entrancy guard for page navigation (see OnPageChanged / LoadPage).
    private bool _isPageLoading = false;

    // Per-page cache of tasks & annotations (keyed by ChapterPageId) so revisiting a
    // data-heavy page does not re-query the DB. The UI mutates these same list
    // instances (Insert/Remove/property edits), so the cache stays in sync in-session.
    private readonly Dictionary<Guid, (List<ProductionTask> Tasks, List<AnnotationModel> Annotations)> _pageDataCache = new();

    // Manual-save buffer for page deletions: a SAVED page removed via the trash button leaves the
    // view immediately but its DB soft-delete is deferred to Save (FlushPendingAsync). Because the
    // row still exists in the DB until then, Discard (reload) brings the page back. A never-saved
    // pending page is just dropped from memory and never reaches this list.
    private readonly List<Guid> _pagesPendingDelete = new();

    private void ToggleRightPanel()
    {
        _isRightPanelOpen = !_isRightPanelOpen;
        StateHasChanged();
    }

    private void ToggleLeftPanel()
    {
        _isLeftPanelOpen = !_isLeftPanelOpen;
        StateHasChanged();
    }

    private async Task DeleteCurrentPage()
    {
        if (IsChapterLocked) return;
        if (UploadedPages.Any())
        {
            var pageToDelete = UploadedPages[ActivePageIndex];

            // Guard: a saved page that has tasks or annotations must not be deleted (deleting it would
            // orphan assigned work / feedback). ActiveTasks/ActiveAnnotations hold the current page's
            // full set (all versions), loaded in LoadPage.
            if (pageToDelete.ChapterPageId != Guid.Empty && (ActiveTasks.Any() || ActiveAnnotations.Any()))
            {
                Snackbar.Add("This page has tasks or annotations and cannot be deleted.", Severity.Warning);
                return;
            }

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
                        // Manual-save: defer the DB soft-delete to Save. The row stays in the DB until
                        // then, so Discard (reload) restores the page. A never-saved pending page
                        // (Guid.Empty) is simply dropped from memory below.
                        _pagesPendingDelete.Add(pageToDelete.ChapterPageId);
                        _pageDataCache.Remove(pageToDelete.ChapterPageId);
                    }

                    UploadedPages.RemoveAt(ActivePageIndex);
                    _saveState = SaveStatus.Dirty;
                    _ = JS.InvokeVoidAsync("setUnsavedFlag", true);

                    var activeChap = Chapters.FirstOrDefault(c => c.Id == SelectedChapter);
                    if (activeChap != null) activeChap.PageCount = UploadedPages.Count;

                    if (UploadedPages.Count == 0)
                    {
                        ActivePageIndex = 0;
                        if (_leftCanvasRef != null)
                            await _leftCanvasRef.InvokeVoidAsync("loadImage", ""); // clear canvas
                        Snackbar.Add("All pages removed (unsaved). Click Save to apply, or Discard to undo.", Severity.Info);
                    }
                    else
                    {
                        if (ActivePageIndex >= UploadedPages.Count)
                            ActivePageIndex = UploadedPages.Count - 1;
                        await LoadPage(ActivePageIndex);
                        Snackbar.Add("Page removed (unsaved). Click Save to apply, or Discard to undo.", Severity.Info);
                    }
                    StateHasChanged();
                }
                catch (Exception ex)
                {
                    Snackbar.Add($"Error deleting page: {ex.Message}", Severity.Error);
                }
            }
        }
    }

    // Returns a version's AUTHORITATIVE saved regions as canvas JSON, read fresh from the DB.
    // Used when switching versions so an older version always shows its true saved state (e.g.
    // v1 original) instead of translation/edits that bled into the in-memory model during the
    // session that produced a newer version. Mirrors the mapping used by the initial load.
    private async Task<string> BuildRegionsJsonFromDbAsync(Guid versionId)
    {
        var dbRegions = await MangakaRegionApi.GetByVersionsAsync(_currentUserId ?? Guid.Empty, new[] { versionId });
        var mapped = new List<RegionModel>();
        int idx = 1;
        foreach (var r in dbRegions)
        {
            if (r.Width <= 0.05m && r.Height <= 0.05m) continue; // skip annotation pin markers

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

            mapped.Add(new RegionModel
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

        var camelOptions = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };
        return mapped.Any()
            ? System.Text.Json.JsonSerializer.Serialize(mapped, camelOptions)
            : "[]";
    }

    private async Task SetTool(string tool)
    {
        CurrentTool = tool;
        await GetActiveCanvas()!.InvokeVoidAsync("setTool", tool);
    }

    private async Task ZoomIn() => await GetActiveCanvas()!.InvokeVoidAsync("zoom", 1.2);
    private async Task ZoomOut() => await GetActiveCanvas()!.InvokeVoidAsync("zoom", 0.8);


    private async Task DeleteSelectedRegions()
    {
        if (IsChapterLocked) return;
        if (!SelectedRegions.Any())
        {
            Snackbar.Add("No region selected.", Severity.Info);
            return;
        }

        // #11 / BR-ANN-017 / BR-PGTASK: a region already linked to a task or annotation must not be
        // deleted (it would orphan open feedback / assigned work). Block it up front with a clear
        // message; the backend (bulk-replace / delete) enforces the same rule on save.
        var linkedRegionDbIds = ActiveTasks.SelectMany(t => t.Regions)
            .Concat(ActiveAnnotations.SelectMany(a => a.Regions))
            .Where(r => r.DbId.HasValue)
            .Select(r => r.DbId!.Value)
            .ToHashSet();
        if (SelectedRegions.Any(r => r.DbId.HasValue && linkedRegionDbIds.Contains(r.DbId.Value)))
        {
            Snackbar.Add("This region cannot be deleted because it is already used by a task or annotation.", Severity.Warning);
            return;
        }

        bool? result = await DialogService.ShowMessageBox(
            "Delete Selected",
            "Are you sure you want to delete the selected panel(s)?",
            yesText: "Delete", cancelText: "Cancel");

        if (result == true && GetActiveCanvas() != null)
        {
            await GetActiveCanvas()!.InvokeVoidAsync("deleteSelectedRegionsConfirmed");
        }
    }

    // #10 region editing: edit the selected region's type + label. Persistence rides the normal Save
    // flow — BulkReplace matches by db id and updates type_code / region_label (preserving the region
    // id so linked tasks/annotations stay intact). Supports batch type-edit: with several regions
    // selected the dialog sets the chosen type on all of them and keeps each region's own label.
    private bool _showEditRegionDialog;
    private string _editRegionType = "PANEL";
    private string? _editRegionLabel;
    private int _editRegionId;
    private int _editRegionCount;

    private void OpenEditRegionDialog()
    {
        if (SelectedRegions.Count < 1) return;
        _editRegionCount = SelectedRegions.Count;
        var first = SelectedRegions[0];
        _editRegionId = first.Id;
        // Prefill the type with the shared value when every selected region already matches; else PANEL.
        var firstType = NormalizeRegionType(first.Type);
        _editRegionType = SelectedRegions.All(r => NormalizeRegionType(r.Type) == firstType) ? firstType : "PANEL";
        // Label is only editable for a single region — a batch keeps each region's existing label.
        _editRegionLabel = _editRegionCount == 1 ? first.Label : null;
        _showEditRegionDialog = true;
    }

    private async Task ApplyEditRegion()
    {
        _showEditRegionDialog = false;
        var canvas = GetActiveCanvas();
        if (canvas == null) return;
        var targets = SelectedRegions.ToList();
        if (targets.Count <= 1)
        {
            await canvas.InvokeVoidAsync("setRegionMeta", _editRegionId, _editRegionType, _editRegionLabel);
        }
        else
        {
            // Batch: apply the chosen type to every selected region, preserving each region's own label
            // (setRegionMeta overwrites the label, so pass the region's current one straight back).
            foreach (var r in targets)
            {
                await canvas.InvokeVoidAsync("setRegionMeta", r.Id, _editRegionType, r.Label);
            }
        }
        Snackbar.Add(targets.Count > 1
            ? $"Type applied to {targets.Count} regions. Click Save to persist the change."
            : "Region updated. Click Save to persist the change.", Severity.Info);
    }

    // Selects every region on the active pane (e.g. to translate or assign the whole page at once).
    private async Task SelectAllRegions()
    {
        if (GetActiveCanvas() != null)
        {
            await GetActiveCanvas()!.InvokeVoidAsync("selectAllRegions");
        }
    }

    // Clears the current region selection on the active pane (e.g. after creating a task/annotation
    // from selected regions, so the next action does not reuse the previous selection).
    private async Task DeselectAllRegions()
    {
        if (GetActiveCanvas() != null)
        {
            await GetActiveCanvas()!.InvokeVoidAsync("clearSelection");
        }
    }

    // Hides/shows the speech-bubble detection frames on both panes (translated text stays visible
    // so the page can be previewed without the editing boxes). Data is untouched.
    private bool _regionsHidden = false;
    private async Task ToggleRegionsVisibility()
    {
        _regionsHidden = !_regionsHidden;
        if (_leftCanvasRef != null)
        {
            await _leftCanvasRef.InvokeVoidAsync("setRegionsVisible", !_regionsHidden);
        }
        if (_rightCanvasRef != null)
        {
            await _rightCanvasRef.InvokeVoidAsync("setRegionsVisible", !_regionsHidden);
        }
    }

    private async Task Undo() => await GetActiveCanvas()!.InvokeVoidAsync("undo");
    private async Task Redo() => await GetActiveCanvas()!.InvokeVoidAsync("redo");

    private async Task RunSegmentAI()
    {
        IsProcessing = true;
        StateHasChanged();
        
        Snackbar.Add("Running Segmentation...", Severity.Info);
        var successSegment = await GetActiveCanvas()!.InvokeAsync<bool>("callSegmentAPI");
        
        IsProcessing = false;
        StateHasChanged();
        
        if (successSegment) {
            Snackbar.Add("Segmentation complete.", Severity.Success);
        } else {
            Snackbar.Add("Segmentation failed.", Severity.Error);
        }
    }

    private async Task RunTranslateAI(string targetLang = "vi")
    {
        var lang = targetLang == "en" ? "en" : "vi";
        IsProcessing = true;
        StateHasChanged();

        Snackbar.Add(lang == "en" ? "Running translation (English)..." : "Running translation (Vietnamese)...", Severity.Info);
        var translateResult = await GetActiveCanvas()!.InvokeAsync<string>("callTranslateAPI", lang);
        
        IsProcessing = false;
        StateHasChanged();
        
        if (translateResult == "success") {
            Snackbar.Add("Translation complete.", Severity.Success);
            if (UploadedPages.Any())
            {
                // Do not overwrite existing version DataUrl; translation overlay remains on canvas until explicitly saved as a new version
            }
        } else {
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
        StateHasChanged();
        // Manual-save model: a real region edit only marks the buffer dirty (indicator +
        // beforeunload guard). Nothing is persisted until the user clicks Save.
        if (page != null && contentChanged)
        {
            _saveState = SaveStatus.Dirty;
            _ = JS.InvokeVoidAsync("setUnsavedFlag", true); // arm the beforeunload guard
        }
    }

    // Called when the brush/clean tool paints the image. The pixels are NOT covered by autosave
    // (only "Save as New Version" uploads a new image to Cloudinary), so flag it so the indicator
    // warns the user and the beforeunload guard is armed.
    public void OnImageEdited(string pane)
    {
        _imageDirty = true;
        _ = JS.InvokeVoidAsync("setUnsavedFlag", true);
        if (!_imageEditHintShown)
        {
            _imageEditHintShown = true;
            Snackbar.Add("Image edited. Click \"Save as New Version\" to keep it — autosave only saves regions/text, not the image.", Severity.Info);
        }
        StateHasChanged();
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





    private async Task ToggleSplitView()
    {
        _isSplitView = !_isSplitView;
        if (_isSplitView)
        {
            _splitUploadedPages = UploadedPages.ToList();
            int targetRight = ActivePageIndex + 1;
            StateHasChanged();
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
            StateHasChanged();
        }
    }

    private void SplitPagePrev()
    {
        if (_splitPageIndex > 0)
        {
            _splitPageIndex--;
            StateHasChanged();
        }
    }

    private void SplitPageNext()
    {
        if (_splitPageIndex < _splitUploadedPages.Count - 1)
        {
            _splitPageIndex++;
            StateHasChanged();
        }
    }

    public void Dispose()
    {
        _objRef?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        _objRef?.Dispose();
        await ValueTask.CompletedTask;
    }

    }
}
