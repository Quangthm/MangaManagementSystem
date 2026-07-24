using static MangaManagementSystem.Web.Components.Pages.Workspace.WorkspaceHelpers;

namespace MangaManagementSystem.Web.Components.Pages.Workspace
{
    public partial class CreatorWorkspace
    {
    private async Task DiscardChangesAsync()
    {
        var chapter = Chapters.FirstOrDefault(c => c.Id == SelectedChapter);
        if (chapter != null)
        {
            chapter.IsPagesLoaded = false;
            await SelectChapter(SelectedChapter);
        }
        _saveState = SaveStatus.Saved;
        _imageDirty = false;
    }

    private async Task<bool> EnsureSavedBeforeAsync(string? actionName = null)
    {
        if (!HasUnsavedChanges) return true;

        bool? result = await DialogService.ShowMessageBox(
            "Unsaved changes",
            "You have unsaved changes. Save before proceeding?",
            yesText: "Save", noText: "Discard", cancelText: "Cancel");

        if (result == null) return false;
        if (result == true)
        {
            await SaveAllChangesAsync();
            // #3: only let the caller proceed (e.g. Submit) if the save actually cleared the buffer.
            // A partial/failed save leaves it dirty — proceeding would submit an incomplete chapter.
            if (HasUnsavedChanges)
            {
                Snackbar.Add("Some changes are still unsaved. Please resolve them before continuing.", Severity.Warning);
                return false;
            }
            return true;
        }

        await DiscardChangesAsync();
        return true;
    }

    // MANUAL SAVE MODEL: there is no autosave timer. Region/structural edits stay in the in-memory
    // buffer (PageVersionModel.Regions + IsDirty, pending chapters/pages) until the user clicks the
    // Save button. SaveAllChangesAsync() flushes the buffer to the DB; the header indicator shows
    // Unsaved changes / Saving… / Saved.
    // Flushes dirty page-version regions to the DB. Called only by the manual Save button now.
    private async Task SaveAllChangesAsync()
    {
        if (_saveState == SaveStatus.Saving) return;

        var pagesToSave = UploadedPages.ToList();
        if (_isSplitView && _splitUploadedPages != null)
        {
            pagesToSave = pagesToSave.Union(_splitUploadedPages).ToList();
        }

        bool hasNewChapters = Chapters.Any(c => c.ChapterId == Guid.Empty);
        bool hasPendingPages = Chapters.Any(c => c.Pages != null && c.Pages.Any(p => p.ChapterPageId == Guid.Empty || (p.Versions.Any() && p.Versions[p.ActiveVersionIndex].PendingBytes != null)));
        bool anyDirty = pagesToSave.Any(p => p != null && p.Versions.Any() && p.Versions[p.ActiveVersionIndex].IsDirty);

        if (!hasNewChapters && !hasPendingPages && !anyDirty && _saveState == SaveStatus.Saved) return;

        _saveState = SaveStatus.Saving;
        StateHasChanged();

        int savedCount = 0;
        int failedCount = 0;
        // #3: count the page uploads up front so the progress indicator can show "page X of Y" during
        // the long-running Cloudinary upload + create loop.
        int totalPendingPages = Chapters
            .Where(c => c.ChapterId != Guid.Empty || c.Pages != null)
            .SelectMany(c => c.Pages ?? new List<PageModel>())
            .Count(p => p.Versions.Any() && p.Versions[p.ActiveVersionIndex].PendingBytes != null);
        int pagesUploaded = 0;
        _uploadRetryCount = 0;
        _saveProgress = totalPendingPages > 0 ? $"Uploading pages… (0/{totalPendingPages})" : "Saving…";
        await _dbSemaphore.WaitAsync();
        try
        {
            if (Guid.TryParse(SeriesId, out Guid seriesGuid))
            {
                foreach (var chap in Chapters.Where(c => c.ChapterId == Guid.Empty).ToList())
                {
                    try
                    {
                        // Persist the user-chosen number label ("2.5" etc.), falling back to the UI key
                        // only if somehow unset. The DB enforces uniqueness incl. cancelled chapters.
                        var numberLabel = string.IsNullOrWhiteSpace(chap.NumberLabel) ? chap.Id.ToString() : chap.NumberLabel.Trim();
                        var req = new CreateChapterDraftRequest(seriesGuid, numberLabel, chap.Title);
                        var createdDto = await MangakaChapterApi.CreateChapterDraftAsync(_currentUserId!.Value, req);
                        if (createdDto != null)
                        {
                            chap.ChapterId = createdDto.ChapterId;
                            chap.StatusCode = createdDto.StatusCode;
                            savedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        Console.WriteLine($"Error creating chapter draft {chap.Id}: {ex.Message}");
                        Snackbar.Add($"Failed to create Chapter {chap.Id}: {ex.Message}", Severity.Error);
                    }
                }
            }

            // Persist buffered chapter renames: existing chapters whose title changed since load. Like the
            // rest of the workspace this is deferred to the manual Save, not written on Enter.
            foreach (var chap in Chapters.Where(c => c.ChapterId != Guid.Empty && c.TitleDirty).ToList())
            {
                try
                {
                    // Send the chapter's real number label ("2.5"), not the positional UI key Id — using
                    // Id would collide with the integer chapter that happens to share that position.
                    var numberLabel = string.IsNullOrWhiteSpace(chap.NumberLabel) ? chap.Id.ToString() : chap.NumberLabel.Trim();
                    await MangakaChapterApi.UpdateChapterDraftAsync(
                        _currentUserId!.Value,
                        chap.ChapterId,
                        new UpdateChapterDraftRequest(
                            numberLabel,
                            string.IsNullOrWhiteSpace(chap.Title) ? null : chap.Title));
                    chap.TitleDirty = false;
                    savedCount++;
                }
                catch (Exception ex)
                {
                    failedCount++;
                    Console.WriteLine($"Error renaming chapter {chap.NumberLabel}: {ex.Message}");
                    Snackbar.Add($"Failed to rename Chapter {chap.NumberLabel}: {ex.Message}", Severity.Error);
                }
            }

            // Flush deferred page soft-deletes. When a SAVED page is removed via the trash button the UI
            // drops it from the buffer immediately but defers the DB soft-delete to here (see DeleteCurrentPage /
            // _pagesPendingDelete). Without this flush the page reappears on reload — the delete never persisted.
            // The delete API applies the same task/annotation guard server-side; the UI already blocks those too.
            if (_pagesPendingDelete.Count > 0 && _currentUserId.HasValue)
            {
                foreach (var pageId in _pagesPendingDelete.ToList())
                {
                    try
                    {
                        await MangakaPageApi.DeleteAsync(_currentUserId.Value, pageId);
                        _pagesPendingDelete.Remove(pageId);
                        savedCount++;
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        Console.WriteLine($"Error deleting page {pageId}: {ex.Message}");
                        Snackbar.Add($"Failed to delete a page: {ex.Message}", Severity.Error);
                    }
                }
            }

            // Phase 1 — upload all pending page images to Cloudinary in PARALLEL (bounded concurrency) so a
            // multi-page save is not bottlenecked by sequential uploads. The DB creates below stay
            // sequential (page-number ordering + orphan cleanup) and just consume these results.
            var pendingVersions = new List<PageVersionModel>();
            foreach (var chap in Chapters)
            {
                if (chap.ChapterId == Guid.Empty || chap.Pages == null) continue;
                foreach (var page in chap.Pages)
                {
                    if (page.Versions.Any())
                    {
                        var v = page.Versions[page.ActiveVersionIndex];
                        if (v.PendingBytes != null) pendingVersions.Add(v);
                    }
                }
            }

            var uploadResults = new System.Collections.Concurrent.ConcurrentDictionary<PageVersionModel, FileUploadResultDto>();
            var uploadErrors = new System.Collections.Concurrent.ConcurrentDictionary<PageVersionModel, string>();
            if (pendingVersions.Count > 0)
            {
                using var uploadGate = new SemaphoreSlim(3);   // at most 3 concurrent Cloudinary uploads
                int uploadedSoFar = 0;
                await Task.WhenAll(pendingVersions.Select(async v =>
                {
                    await uploadGate.WaitAsync();
                    try
                    {
                        uploadResults[v] = await UploadPageImageWithRetryAsync(v);
                    }
                    catch (Exception ex)
                    {
                        uploadErrors[v] = ex.Message;
                    }
                    finally
                    {
                        uploadGate.Release();
                        var n = System.Threading.Interlocked.Increment(ref uploadedSoFar);
                        _saveProgress = $"Uploading pages… ({n}/{pendingVersions.Count})";
                        await InvokeAsync(StateHasChanged);
                    }
                }));
            }

            foreach (var chap in Chapters)
            {
                if (chap.ChapterId == Guid.Empty || chap.Pages == null) continue;
                // New pages continue AFTER the highest existing page_no in this chapter — NOT a positional
                // index. A soft-deleted page frees its number but leaves a gap; positional numbering would
                // re-hit a still-active page_no and violate the filtered unique index
                // ux_chapter_page_active_page_no ("Could not create page with version"). Gaps are kept and
                // never reused (consistent with cancelled chapter numbers). PageNo == 0 = a not-yet-saved
                // pending page that does not occupy a number yet.
                int nextNewPageNo = chap.Pages.Where(p => p.PageNo > 0).Select(p => p.PageNo).DefaultIfEmpty(0).Max() + 1;
                foreach (var page in chap.Pages)
                {
                    if (page.ChapterPageId == Guid.Empty || (page.Versions.Any() && page.Versions[page.ActiveVersionIndex].PendingBytes != null))
                    {
                        if (page.Versions.Any())
                        {
                            var ver = page.Versions[page.ActiveVersionIndex];
                            if (ver.PendingBytes != null)
                            {
                                _saveProgress = $"Saving pages… ({pagesUploaded + 1}/{totalPendingPages})";
                                StateHasChanged();
                                // Images were already uploaded to Cloudinary in parallel (Phase 1); consume
                                // the result here. Track the public id so an orphan file can be best-effort
                                // cleaned up if the DB create then fails.
                                string? uploadedPublicId = null;
                                try
                                {
                                    if (uploadErrors.TryGetValue(ver, out var uploadErr))
                                        throw new InvalidOperationException(uploadErr);
                                    if (!uploadResults.TryGetValue(ver, out var uploadResult))
                                        throw new InvalidOperationException("Upload result was not found for this page.");
                                    uploadedPublicId = uploadResult.PublicId;

                                    var fileDto = new CreateFileResourceDto(
                                        "CHAPTER_PAGE_VERSION",
                                        uploadResult.OriginalFileName,
                                        uploadResult.PublicId,
                                        uploadResult.SecureUrl,
                                        uploadResult.ContentType,
                                        uploadResult.FileSizeBytes,
                                        uploadResult.Sha256Hash,
                                        _currentUserId);

                                    if (page.ChapterPageId == Guid.Empty)
                                    {
                                        // New page: create page + version 1 + file atomically.
                                        var createReq = new CreatePageWithVersionRequestDto(
                                            chap.ChapterId,
                                            nextNewPageNo,
                                            null,
                                            fileDto,
                                            ver.Note ?? "Original Upload");

                                        var createdRes = await MangakaPageApi.CreatePageWithVersionAsync(_currentUserId!.Value, createReq);
                                        if (createdRes != null)
                                        {
                                            page.ChapterPageId = createdRes.Page.ChapterPageId;
                                            page.PageNo = createdRes.Page.PageNo;   // authoritative number just assigned
                                            ver.ChapterPageVersionId = createdRes.Version.ChapterPageVersionId;
                                            ver.DataUrl = uploadResult.SecureUrl;
                                            ver.PendingBytes = null;
                                            ver.IsDirty = false;
                                            savedCount++;
                                            pagesUploaded++;
                                            nextNewPageNo++;   // the next new page continues after this one
                                        }
                                    }
                                    else
                                    {
                                        // #5: a pending version on an EXISTING page (deferred "Upload Version").
                                        // Create the version + file + its regions atomically and set current —
                                        // NOT a new page (that would duplicate the page).
                                        var req = new CreateVersionWithFileAndRegionsRequestDto(
                                            ChapterPageId: page.ChapterPageId,
                                            VersionNo: (short)ver.VersionNo,
                                            FileDto: fileDto,
                                            VersionNote: ver.Note ?? $"Uploaded Version {ver.VersionNo}",
                                            Regions: BuildRegionDtosForSave(ver.Regions),
                                            SetAsCurrent: true);

                                        var versionDto = await MangakaPageApi.CreateVersionWithFileAndRegionsAsync(_currentUserId!.Value, req);
                                        if (versionDto != null)
                                        {
                                            ver.ChapterPageVersionId = versionDto.ChapterPageVersionId;
                                            ver.DataUrl = uploadResult.SecureUrl;
                                            ver.PendingBytes = null;
                                            ver.IsDirty = false;
                                            foreach (var v in page.Versions) v.IsCurrentVersion = ReferenceEquals(v, ver);
                                            savedCount++;
                                            pagesUploaded++;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    failedCount++;
                                    // The DB create rolled back; the Cloudinary upload (if it succeeded) is
                                    // now an orphan — best-effort delete so we do not leave a dangling file.
                                    if (!string.IsNullOrEmpty(uploadedPublicId))
                                    {
                                        try { await FileStorageService.DeleteFileAsync(uploadedPublicId, "image"); } catch { }
                                    }
                                    var failedPageNo = page.PageNo > 0 ? page.PageNo : nextNewPageNo;
                                    Console.WriteLine($"Error uploading page {failedPageNo}: {ex.Message}");
                                    Snackbar.Add($"Failed to upload page {failedPageNo}: {ex.Message}", Severity.Error);
                                }
                            }
                        }
                    }
                }
            }

            // Save dirty region edits on EVERY loaded page across ALL chapters, not just the current
            // chapter's (UploadedPages = the selected chapter only). A region edit made on a page of a
            // DIFFERENT chapter earlier in the session was otherwise never persisted and left a permanent
            // "dirty" flag -> the phantom "some changes could not be saved (0 failed)" that only a reload
            // cleared. Split-view pages reference the same PageModel objects, so Distinct() de-dupes them.
            var allRegionPages = Chapters.Where(c => c.Pages != null).SelectMany(c => c.Pages)
                .Concat(_isSplitView && _splitUploadedPages != null ? _splitUploadedPages : Enumerable.Empty<PageModel>())
                .Distinct()
                .ToList();
            foreach (var page in allRegionPages)
            {
                if (page == null) continue;
                if (page.Versions.Any())
                {
                    var currentVersion = page.Versions[page.ActiveVersionIndex];
                    if (!currentVersion.IsDirty) continue;
                    if (currentVersion.ChapterPageVersionId == Guid.Empty)
                    {
                        // Region edits attach to a SAVED version. If the active version has no DB id yet and
                        // no pending image to upload (e.g. a system FULL_PAGE anchor got nudged, or a stale
                        // placeholder), its region change can never be persisted on its own — clear the
                        // spurious dirty flag so the manual Save is not blocked forever ("0 saved" loop).
                        if (currentVersion.PendingBytes == null) currentVersion.IsDirty = false;
                        continue;
                    }
                    if (!string.IsNullOrEmpty(currentVersion.Regions))
                    {
                        try
                        {
                            var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                            var allRegions = System.Text.Json.JsonSerializer.Deserialize<List<RegionModel>>(currentVersion.Regions, options);
                            
                            if (allRegions != null)
                            {
                                var dtos = allRegions.Select(r => {
                                    var textJson = System.Text.Json.JsonSerializer.Serialize(new {
                                        original = r.OriginalText ?? "",
                                        translated = r.TranslatedText ?? ""
                                    });

                                    string label = !string.IsNullOrEmpty(r.Label) ? r.Label : $"Region_{r.Id}";
                                    var (sourceType, confidence) = NormalizeRegionProvenance(r);
                                    // Defensive clamp: coordinates must be >= 0 (DB CHECK
                                    // ck_page_region_coordinates_nonnegative). A region dragged into the gray
                                    // margin could carry a negative x/y; without this the whole BulkReplace
                                    // fails with a 500 and the page stays unsaved. The canvas also clamps now,
                                    // but this guards any already-buffered bad value too.
                                    return new MangaManagementSystem.Application.DTOs.Manga.CreatePageRegionDto(
                                        currentVersion.ChapterPageVersionId,
                                        (r.Type ?? "OTHER").ToUpper(),
                                        label,
                                        Math.Max(0m, (decimal)r.X),
                                        Math.Max(0m, (decimal)r.Y),
                                        Math.Max(0m, (decimal)r.Width),
                                        Math.Max(0m, (decimal)r.Height),
                                        confidence,
                                        sourceType,
                                        textJson,
                                        r.DbId,
                                        _currentUserId
                                    );
                                }).ToList();

                                await MangakaRegionApi.BulkReplaceAsync(_currentUserId ?? Guid.Empty, currentVersion.ChapterPageVersionId, dtos);
                                currentVersion.IsDirty = false;
                                savedCount++;
                            }
                            else
                            {
                                // Payload present but not a valid array: nothing to persist. Clear the dirty
                                // flag so the honest check below does not report "0 failed but not saved".
                                currentVersion.IsDirty = false;
                            }
                        }
                        catch (Exception ex)
                        {
                            failedCount++;
                            Console.WriteLine($"Error saving regions: {ex.Message}");
                            Snackbar.Add($"Failed to save regions for a page: {ex.Message}", Severity.Error);
                        }
                    }
                    else
                    {
                        // Dirty version with a DB id but no region payload (a spurious / left-over flag,
                        // e.g. after poking error paths): nothing to persist -> clear it so Save does not
                        // report a phantom "some changes could not be saved (0 failed)".
                        currentVersion.IsDirty = false;
                    }
                }
            }

            // #3: be honest about the outcome. Anything still pending (a never-saved page/chapter or a
            // dirty version) means the save did not fully succeed. Only report success + clear the dirty
            // state when NOTHING failed and NOTHING remains pending — otherwise keep the buffer dirty so
            // the Save button stays enabled and the user can retry, instead of a false "saved" that also
            // wrongly lets Submit proceed.
            bool anyRemainingUnsaved =
                Chapters.Any(c => c.ChapterId == Guid.Empty) ||
                Chapters.Any(c => c.TitleDirty) ||
                Chapters.Any(c => c.Pages != null && c.Pages.Any(p =>
                    p.ChapterPageId == Guid.Empty ||
                    (p.Versions.Any() && p.Versions[p.ActiveVersionIndex].PendingBytes != null) ||
                    (p.Versions.Any() && p.Versions[p.ActiveVersionIndex].IsDirty))) ||
                _pagesPendingDelete.Count > 0;

            if (failedCount == 0 && !anyRemainingUnsaved)
            {
                _lastSavedAtUtc = DateTime.UtcNow;
                _saveState = SaveStatus.Saved;
                _imageDirty = false;
                _ = JS.InvokeVoidAsync("setUnsavedFlag", false);
                if (savedCount > 0)
                {
                    Snackbar.Add(
                        _uploadRetryCount > 0
                            ? $"Changes saved successfully (after {_uploadRetryCount} upload retry/retries)."
                            : "Changes saved successfully.",
                        Severity.Success);
                }
            }
            else
            {
                // Partial/failed save: keep it dirty and tell the user exactly what happened.
                _saveState = SaveStatus.Dirty;
                _ = JS.InvokeVoidAsync("setUnsavedFlag", true);
                Snackbar.Add(
                    $"Some changes could not be saved ({failedCount} failed, {savedCount} saved). Please click Save to try again.",
                    Severity.Warning);
            }
        }
        finally
        {
            _saveProgress = null;
            _dbSemaphore.Release();
            StateHasChanged();
        }
    }

    // --- Upload retry -------------------------------------------------------------------------
    // A Cloudinary upload can fail on a transient network hiccup / timeout while the bytes and the
    // DB row are both still fine. Retrying the whole Save in that case re-uploads every page, so
    // each page image gets its own bounded retry with exponential backoff first.
    private const int UploadMaxAttempts = 3;

    // Number of retried upload attempts in the current save (for the summary snackbar).
    private int _uploadRetryCount;

    private async Task<FileUploadResultDto> UploadPageImageWithRetryAsync(PageVersionModel version)
    {
        Exception? lastError = null;
        for (int attempt = 1; attempt <= UploadMaxAttempts; attempt++)
        {
            try
            {
                return await FileStorageService.UploadFileAsync(
                    version.PendingBytes!,
                    version.PendingFileName ?? "page.png",
                    version.PendingContentType ?? "image/png",
                    "CHAPTER_PAGE_VERSION");
            }
            catch (Exception ex) when (attempt < UploadMaxAttempts && IsTransientUploadError(ex))
            {
                lastError = ex;
                System.Threading.Interlocked.Increment(ref _uploadRetryCount);
                Console.WriteLine($"Upload attempt {attempt}/{UploadMaxAttempts} failed for " +
                                  $"{version.PendingFileName}: {ex.Message}");
                _saveProgress = $"Upload failed — retrying ({attempt + 1}/{UploadMaxAttempts})…";
                await InvokeAsync(StateHasChanged);
                // 400ms, 800ms — short enough that a save does not feel hung.
                await Task.Delay(TimeSpan.FromMilliseconds(400 * Math.Pow(2, attempt - 1)));
            }
        }

        throw lastError ?? new InvalidOperationException("Upload failed.");
    }

    // Only network-shaped failures are worth retrying; a rejected/invalid file would fail again.
    private static bool IsTransientUploadError(Exception ex)
    {
        for (Exception? e = ex; e != null; e = e.InnerException)
        {
            if (e is HttpRequestException
                || e is TaskCanceledException
                || e is TimeoutException
                || e is System.IO.IOException
                || e is System.Net.Sockets.SocketException)
            {
                return true;
            }

            var msg = e.Message;
            if (!string.IsNullOrEmpty(msg)
                && (msg.Contains("timeout", StringComparison.OrdinalIgnoreCase)
                    || msg.Contains("timed out", StringComparison.OrdinalIgnoreCase)
                    || msg.Contains("temporarily", StringComparison.OrdinalIgnoreCase)
                    || msg.Contains("Service Unavailable", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    // A bulk save rewrites every region of the version, so it must carry each box's own provenance
    // back — sending a flat "MANUAL" would silently downgrade AI-detected regions and drop their
    // confidence score on the first save of an untouched page.
    // manga.PageRegion.ck_page_region_confidence_source only accepts AI + a score, or MANUAL + no
    // score, so a region claiming AI without a usable score is stored as MANUAL instead of failing
    // the whole save.
    private static (string SourceType, decimal? ConfidenceScore) NormalizeRegionProvenance(RegionModel region)
    {
        var sourceType = (region.SourceType ?? string.Empty).Trim().ToUpperInvariant();
        if (sourceType != "AI") return ("MANUAL", null);

        // ck_page_region_confidence: 0 <= confidence_score <= 1.
        if (region.ConfidenceScore is not double score || double.IsNaN(score) || score < 0 || score > 1)
        {
            return ("MANUAL", null);
        }

        return ("AI", (decimal)score);
    }

    }
}
