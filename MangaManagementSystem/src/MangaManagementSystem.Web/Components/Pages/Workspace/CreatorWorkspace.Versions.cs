using static MangaManagementSystem.Web.Components.Pages.Workspace.WorkspaceHelpers;

namespace MangaManagementSystem.Web.Components.Pages.Workspace
{
    public partial class CreatorWorkspace
    {
    private async Task SwitchVersion(string pane, int versionIndex, bool isDeleting = false)
    {
        var page = pane == "Left" ? UploadedPages.ElementAtOrDefault(ActivePageIndex) : _splitUploadedPages.ElementAtOrDefault(_splitPageIndex);
        var canvas = pane == "Left" ? _leftCanvasRef : _rightCanvasRef;

        if (page == null) return;
        // Manual-save model: do NOT auto-persist on switch. Unsaved region edits of the version we
        // are leaving stay in its in-memory buffer.
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

                // Load the version's AUTHORITATIVE regions from the DB so an older version shows its
                // original saved state. BUT in the manual-save model we must NOT clobber a version
                // that has unsaved in-memory edits (IsDirty) — reloading from the DB would discard
                // the user's unsaved buffer. Only reload clean (non-dirty), already-saved versions.
                if (!activeVersion.IsDeleted
                    && activeVersion.ChapterPageVersionId != Guid.Empty
                    && !activeVersion.IsDirty)
                {
                    activeVersion.Regions = await BuildRegionsJsonFromDbAsync(activeVersion.ChapterPageVersionId);
                }
                // Programmatic load: silent=true so switching versions does not mark
                // the page dirty or write a phantom draft.
                await canvas.InvokeVoidAsync("loadRegions",
                    string.IsNullOrEmpty(activeVersion.Regions) ? "[]" : activeVersion.Regions, true);
                CanUndo = false;
                CanRedo = false;
            }
            // Tasks/annotations are version-scoped (Option B): re-sync the canvas pins so only the
            // newly selected version's annotations show, and re-render the Task Panel lists.
            await SyncAnnotationsToJS();
            StateHasChanged();
        }
    }

    private async Task SaveAsNewVersion(string pane)
    {
        var page = pane == "Left" ? UploadedPages.ElementAtOrDefault(ActivePageIndex) : _splitUploadedPages.ElementAtOrDefault(_splitPageIndex);
        var canvas = pane == "Left" ? _leftCanvasRef : _rightCanvasRef;

        if (page != null && canvas != null)
        {
            IsProcessing = true;
            StateHasChanged();
            Snackbar.Add("Buffering new version...", Severity.Info);

            try
            {
                var dataUrl = await canvas.InvokeAsync<string>("exportImage");
                int nextVersionNo = page.Versions.Any() ? page.Versions.Max(v => v.VersionNo) + 1 : 1;

                var commaIndex = dataUrl.IndexOf(',');
                var base64Data = dataUrl.Substring(commaIndex + 1);
                var bytes = Convert.FromBase64String(base64Data);

                var currentRegionsJson = await canvas.InvokeAsync<string>("exportRegions");

                var oldVer = page.Versions.ElementAtOrDefault(page.ActiveVersionIndex);
                if (oldVer != null) oldVer.IsDirty = false;

                foreach (var v in page.Versions) v.IsCurrentVersion = false;
                
                var newVersionModel = new PageVersionModel
                {
                    ChapterPageVersionId = Guid.Empty, // IsPending = true
                    VersionNo = nextVersionNo,
                    Regions = currentRegionsJson,
                    DataUrl = dataUrl,
                    Note = $"New Version {nextVersionNo}",
                    IsCurrentVersion = true,
                    PendingBytes = bytes,
                    PendingFileName = $"page_{SelectedPage}_v{nextVersionNo}.png",
                    PendingContentType = "image/png"
                };
                
                page.Versions.Add(newVersionModel);
                page.ActiveVersionIndex = page.Versions.Count - 1;

                // Silent load → no phantom dirty/draft; reset undo history.
                await canvas.InvokeVoidAsync("loadImage", dataUrl);
                await canvas.InvokeVoidAsync("loadRegions", string.IsNullOrEmpty(currentRegionsJson) ? "[]" : currentRegionsJson, true);
                CanUndo = false;
                CanRedo = false;

                _saveState = SaveStatus.Dirty;
                _ = JS.InvokeVoidAsync("setUnsavedFlag", true);
                Snackbar.Add($"Created New Version {nextVersionNo} (Unsaved)", Severity.Success);
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                Snackbar.Add($"Failed to buffer version: {msg}", Severity.Error);
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                IsProcessing = false;
                StateHasChanged();
            }
        }
    }

    private async Task HandleUploadVersion(InputFileChangeEventArgs e, string pane)
    {
        if (IsChapterLocked) return;
        var page = pane == "Left" ? UploadedPages.ElementAtOrDefault(ActivePageIndex) : _splitUploadedPages.ElementAtOrDefault(_splitPageIndex);
        var canvas = pane == "Left" ? _leftCanvasRef : _rightCanvasRef;
        if (page == null || canvas == null) return;

        var file = e.File;
        if (file == null) return;

        if (!IsAllowedWorkspaceImage(file, out var fileError))
        {
            Snackbar.Add(fileError, Severity.Warning);
            return;
        }

        try
        {
            using var stream = file.OpenReadStream(maxAllowedSize: WorkspaceMaxFileSizeBytes);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var bytes = memoryStream.ToArray();
            var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "image/png" : file.ContentType;
            var dataUrl = $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";

            const string currentRegionsJson = "[]";

            _addPagesStaged = new List<StagedImage>
            {
                new StagedImage { Bytes = bytes, Name = file.Name, ContentType = contentType, DataUrl = dataUrl }
            };

            ShowUploadConfirm(
                "Add new version",
                new List<string>(), // No thumbs needed, using _addPagesStaged
                async () =>
                {
                    var staged = _addPagesStaged.FirstOrDefault();
                    if (staged == null) return;

                    int nextVersionNo = page.Versions.Any() ? page.Versions.Max(v => v.VersionNo) + 1 : 1;
                    foreach (var v in page.Versions) v.IsCurrentVersion = false;
                    page.Versions.Add(new PageVersionModel
                    {
                        VersionNo = nextVersionNo,
                        DataUrl = staged.DataUrl,
                        Regions = currentRegionsJson,
                        Note = $"Uploaded Version {nextVersionNo}",
                        ChapterPageVersionId = Guid.Empty,   // pending until Save
                        IsCurrentVersion = true,
                        IsDirty = false,
                        PendingBytes = staged.Bytes,
                        PendingFileName = staged.Name,
                        PendingContentType = staged.ContentType
                    });
                    page.ActiveVersionIndex = page.Versions.Count - 1;
                    
                    await canvas.InvokeVoidAsync("loadImage", staged.DataUrl);
                    await canvas.InvokeVoidAsync("loadRegions", string.IsNullOrEmpty(currentRegionsJson) ? "[]" : currentRegionsJson, true);
                    CanUndo = false;
                    CanRedo = false;
                    
                    _saveState = SaveStatus.Dirty;
                    _ = JS.InvokeVoidAsync("setUnsavedFlag", true);
                    
                    Snackbar.Add($"New Version {nextVersionNo} added (unsaved). Click Save to upload & persist.", Severity.Info);
                }
            );
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error reading version image: {ex.InnerException?.Message ?? ex.Message}", Severity.Error);
        }
    }

    // #4 upload review dialog: preview the selected image(s) as small thumbnails and only accept on Add.
    // Thumbnails (window.mmsMakeThumbnails, in the always-loaded upload-preview.js) keep the base64
    // payload tiny so the Blazor Server render never freezes the dialog. The dialog is closed BEFORE
    // the callback runs so it can never appear stuck.
    private bool _showUploadConfirm;
    private string _uploadConfirmTitle = "Confirm upload";
    private List<string> _uploadConfirmPreviews = new();
    private Func<Task>? _uploadConfirmOnConfirm;

    private async Task<List<string>> BuildPreviewThumbnailsAsync(IEnumerable<string> dataUrls)
    {
        try
        {
            var thumbs = await JS.InvokeAsync<string[]>("mmsMakeThumbnails", dataUrls.ToArray(), 240);
            return thumbs?.Where(t => !string.IsNullOrEmpty(t)).ToList() ?? new List<string>();
        }
        catch
        {
            return new List<string>();   // preview is best-effort; the dialog still works without it
        }
    }

    private void ShowUploadConfirm(string title, List<string> previews, Func<Task> onConfirm)
    {
        _uploadConfirmTitle = title;
        _uploadConfirmPreviews = previews;
        _uploadConfirmOnConfirm = onConfirm;
        _showUploadConfirm = true;
        StateHasChanged();
    }

    private void CancelUploadConfirm()
    {
        _showUploadConfirm = false;
        _uploadConfirmPreviews = new();
        _uploadConfirmOnConfirm = null;
        _addPagesStaged = new();
    }

    private async Task ConfirmUpload()
    {
        // Close the dialog first so it can never appear stuck while the callback runs.
        _showUploadConfirm = false;
        var cb = _uploadConfirmOnConfirm;
        _uploadConfirmOnConfirm = null;
        _uploadConfirmPreviews = new();
        StateHasChanged();
        if (cb != null) await cb();
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
        StateHasChanged();

        try
        {
            // Guard (active task / unresolved annotation) + FileResource soft-delete + audit are all
            // owned by the service. The version row and its regions are kept as a history placeholder.
            var delResult = await MangakaPageApi.DeleteVersionImageAsync(
                activeVer.ChapterPageVersionId);
            if (delResult == null) return;

            if (!delResult.Success)
            {
                Snackbar.Add(delResult.BlockedReason ?? "Could not delete the image.", Severity.Warning);
                return;
            }

            // Best-effort Cloudinary cleanup (no open DB transaction held).
            if (!string.IsNullOrEmpty(delResult.CloudinaryPublicId))
            {
                try { await FileStorageService.DeleteFileAsync(delResult.CloudinaryPublicId, "image"); } catch { }
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
            StateHasChanged();
        }
    }

    private async Task SetActiveVersion(string pane, PageVersionModel version)
    {
        if (IsChapterLocked) return;
        if (version.IsCurrentVersion) return;

        var page = pane == "Left" ? UploadedPages.ElementAtOrDefault(ActivePageIndex) : _splitUploadedPages.ElementAtOrDefault(_splitPageIndex);
        if (page == null) return;

        IsProcessing = true;
        StateHasChanged();
        Snackbar.Add("Setting active version...", Severity.Info);

        try
        {
            await MangakaPageApi.SetCurrentVersionAsync(page.ChapterPageId, version.ChapterPageVersionId);
            
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
            StateHasChanged();
        }
    }

    }
}
