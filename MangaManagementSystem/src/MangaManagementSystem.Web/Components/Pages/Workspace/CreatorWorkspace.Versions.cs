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
        _imageDirty = false;
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
        // A new version attaches to a saved ChapterPage (BR-CP-014/021). If the page is still in the
        // buffer, persist it first (this saves version 1 = original upload); the brush edit then
        // becomes version 2.
        if (!await EnsureSavedBeforeAsync()) return;

        var page = pane == "Left" ? UploadedPages.ElementAtOrDefault(ActivePageIndex) : _splitUploadedPages.ElementAtOrDefault(_splitPageIndex);
        var canvas = pane == "Left" ? _leftCanvasRef : _rightCanvasRef;

        if (page != null && canvas != null)
        {
            IsProcessing = true;
            StateHasChanged();
            Snackbar.Add("Saving new version to cloud...", Severity.Info);

            string? uploadedPublicId = null;
            try
            {
                var dataUrl = await canvas.InvokeAsync<string>("exportImage");

                int nextVersionNo = page.Versions.Any() ? page.Versions.Max(v => v.VersionNo) + 1 : 1;

                var commaIndex = dataUrl.IndexOf(',');
                var base64Data = dataUrl.Substring(commaIndex + 1);
                var bytes = Convert.FromBase64String(base64Data);

                // 1. Upload to Cloudinary FIRST (no SQL transaction held during upload).
                var uploadResult = await FileStorageService.UploadFileAsync(bytes, $"page_{SelectedPage}_v{nextVersionNo}.png", "image/png", "CHAPTER_PAGE_VERSION");
                uploadedPublicId = uploadResult.PublicId;

                var currentRegionsJson = await canvas.InvokeAsync<string>("exportRegions");
                var caseInsensitiveOpts = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var currentRegions = System.Text.Json.JsonSerializer.Deserialize<List<RegionModel>>(currentRegionsJson, caseInsensitiveOpts);

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
                var req = new CreateVersionWithFileAndRegionsRequestDto(
                    ChapterPageId: page.ChapterPageId,
                    VersionNo: (short)nextVersionNo,
                    FileDto: fileDto,
                    VersionNote: $"New Version {nextVersionNo}",
                    Regions: regionDtos,
                    SetAsCurrent: true
                );
                ChapterPageVersionDto? versionDto = null;
                try
                {
                    versionDto = await MangakaPageApi.CreateVersionWithFileAndRegionsAsync(req);
                }
                catch
                {
                    try { await FileStorageService.DeleteFileAsync(uploadResult.PublicId, "image"); } catch { }
                    throw;
                }
                if (versionDto == null)
                {
                    try { await FileStorageService.DeleteFileAsync(uploadResult.PublicId, "image"); } catch { }
                    throw new InvalidOperationException("Failed to create version.");
                }

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

                // The brushed image is now persisted as a new Cloudinary version → image is clean.
                _imageDirty = false;
                _saveState = SaveStatus.Saved;
                _lastSavedAtUtc = DateTime.UtcNow;
                _ = JS.InvokeVoidAsync("setUnsavedFlag", false);
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
                StateHasChanged();
            }
        }
    }

    private async Task HandleUploadVersion(InputFileChangeEventArgs e, string pane)
    {
        if (IsChapterLocked) return;
        // A new version attaches to a saved ChapterPage; persist a pending page first.
        if (!await EnsureSavedBeforeAsync()) return;
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

            // Carry the current canvas regions onto the new version (same behaviour as before).
            var currentRegionsJson = await canvas.InvokeAsync<string>("exportRegions");

            // #4 review + #5 defer: show the thumbnail confirm dialog; on Add, buffer as a PENDING new
            // version. Like a page upload, nothing hits Cloudinary/DB until Save → FlushPendingAsync
            // creates the version (best-effort Cloudinary cleanup on failure).
            var thumbs = await BuildPreviewThumbnailsAsync(new[] { dataUrl });
            ShowUploadConfirm(
                "Add new version",
                thumbs,
                async () =>
                {
                    int nextVersionNo = page.Versions.Any() ? page.Versions.Max(v => v.VersionNo) + 1 : 1;
                    foreach (var v in page.Versions) v.IsCurrentVersion = false;
                    page.Versions.Add(new PageVersionModel
                    {
                        VersionNo = nextVersionNo,
                        DataUrl = dataUrl,
                        Regions = currentRegionsJson,
                        Note = $"Uploaded Version {nextVersionNo}",
                        ChapterPageVersionId = Guid.Empty,   // pending until Save
                        IsCurrentVersion = true,
                        IsDirty = false,
                        PendingBytes = bytes,
                        PendingFileName = file.Name,
                        PendingContentType = contentType
                    });
                    page.ActiveVersionIndex = page.Versions.Count - 1;
                    _saveState = SaveStatus.Dirty;
                    _ = JS.InvokeVoidAsync("setUnsavedFlag", true);

                    await canvas.InvokeVoidAsync("loadImage", dataUrl);
                    await canvas.InvokeVoidAsync("loadRegions", string.IsNullOrEmpty(currentRegionsJson) ? "[]" : currentRegionsJson, true);
                    CanUndo = false;
                    CanRedo = false;
                    Snackbar.Add($"New Version {nextVersionNo} added (unsaved). Click Save to upload & persist.", Severity.Info);
                });
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
