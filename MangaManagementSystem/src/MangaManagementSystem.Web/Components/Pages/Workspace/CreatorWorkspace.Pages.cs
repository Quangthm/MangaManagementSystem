using static MangaManagementSystem.Web.Components.Pages.Workspace.WorkspaceHelpers;

namespace MangaManagementSystem.Web.Components.Pages.Workspace
{
    public partial class CreatorWorkspace
    {
    private async Task LoadPage(int index)
    {
        if (index < 0 || index >= UploadedPages.Count) return;
        if (_isPageLoading) return;
        _isPageLoading = true;
        // Loading a saved version's image = clean baseline; any unsaved brush on the previous
        // view was not persisted and is being navigated away from.
        _imageDirty = false;
        try
        {
            var page = UploadedPages[index];

            ActivePageIndex = index;
            // Use the TRUE DB page number (ChapterPage.page_number), not the list position — soft-deletes
            // leave gaps, so position can differ from page_number and annotations/tasks (keyed by the real
            // page) would otherwise appear under the wrong page number. Fallback to position for pending
            // pages not yet saved (PageNo == 0).
            SelectedPage = page.PageNo > 0 ? page.PageNo : index + 1;

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
                    await _dbSemaphore.WaitAsync();
                    try
                    {
                        // Map PageRegionId -> the panel number the canvas shows (#N) for the active
                        // version, so task/annotation targets display the same number the user sees.
                        var canvasPanelNo = new Dictionary<Guid, int>();
                        var activeVerForNo = page.Versions.ElementAtOrDefault(page.ActiveVersionIndex);
                        if (activeVerForNo != null && !string.IsNullOrEmpty(activeVerForNo.Regions))
                        {
                            try
                            {
                                var caseInsensitiveOpts = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                                var canvasRegions = System.Text.Json.JsonSerializer.Deserialize<List<RegionModel>>(
                                    activeVerForNo.Regions, caseInsensitiveOpts);
                                if (canvasRegions != null)
                                    foreach (var cr in canvasRegions)
                                        if (cr.DbId.HasValue) canvasPanelNo[cr.DbId.Value] = cr.Id;
                            }
                            catch { }
                        }

                        var dbTasks = await MangakaTaskApi.GetTasksByPageAsync(_currentUserId!.Value, page.ChapterPageId);
                        int idCounter = 1;
                        foreach (var t in dbTasks)
                        {
                            var statusMap = t.StatusCode switch
                            {
                                "ASSIGNED" => "Assigned",
                                "UNDER_REVIEW" => "In Review",
                                "COMPLETED" => "Completed",
                                "CANCELLED" => "Cancelled",
                                _ => "Assigned"
                            };
                            var taskPanels = t.PageRegions?.Where(r => r.Width > 0.05m)
                                .Select(r => canvasPanelNo.TryGetValue(r.PageRegionId, out var no)
                                    ? $"Panel #{no}"
                                    : $"Panel #{ParseRegionId(r.RegionLabel)}")
                                .ToList() ?? new List<string>();
                            string taskTarget = taskPanels.Any() ? string.Join(", ", taskPanels) : $"Page {SelectedPage}";
                            newTasks.Add(new ProductionTask
                            {
                                Id = idCounter++,
                                DbId = t.ChapterPageTaskId,
                                Type = t.TypeCode,
                                Assistant = t.AssignedUsername ?? t.AssignedToDisplayName ?? t.AssignedToUserId.ToString(),
                                Target = taskTarget,
                                Description = t.TaskDescription,
                                Status = statusMap,
                                DueAtUtc = t.DueAtUtc,
                                VersionId = t.PageRegions?.FirstOrDefault()?.ChapterPageVersionId,
                                Regions = t.PageRegions?.Select(r => new RegionModel {
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

                        var dbAnnotations = await MangakaAnnotationApi.GetByPageAsync(_currentUserId!.Value, page.ChapterPageId);
                        int annIdCounter = 1;
                        foreach (var a in dbAnnotations)
                        {
                              var pinRegion = a.PageRegions?.FirstOrDefault(r => r.Width <= 0.05m && r.Height <= 0.05m);
                              var targetRegions = a.PageRegions?.Where(r => r.Width > 0.05m)
                                  .Select(r => canvasPanelNo.TryGetValue(r.PageRegionId, out var no)
                                      ? $"Panel #{no}"
                                      : $"Panel #{ParseRegionId(r.RegionLabel)}")
                                  .ToList() ?? new List<string>();

                            string annTarget;
                            if (pinRegion != null)
                                annTarget = $"Pin at ({Math.Round(pinRegion.X)}, {Math.Round(pinRegion.Y)})";
                            else if (targetRegions.Any())
                                annTarget = string.Join(", ", targetRegions);
                            else
                                annTarget = $"Page {SelectedPage}";

                            newAnnotations.Add(new AnnotationModel
                            {
                                Id = annIdCounter++,
                                DbId = a.ChapterPageAnnotationId,
                                Type = a.IssueTypeCode,
                                Comment = a.AnnotationText ?? "",
                                Target = annTarget,
                                PageNumber = SelectedPage,
                                PinX = pinRegion != null ? (double?)pinRegion.X : null,
                                PinY = pinRegion != null ? (double?)pinRegion.Y : null,
                                IsResolved = a.ResolvedByUserId != null,
                                VersionId = a.PageRegions?.FirstOrDefault()?.ChapterPageVersionId,
                                Regions = a.PageRegions?.Select(r => new RegionModel {
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
                        _dbSemaphore.Release();
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
            StateHasChanged();

            // Warm the browser cache for the next/previous page images so paging
            // through a chapter feels instant after the first view. Fire-and-forget
            // so it never blocks navigation or holds the load guard.
            _ = PreloadAdjacentPagesAsync(index);
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

    }
}
