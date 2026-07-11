using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using MangaManagementSystem.Application.Common;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Web.Services.Api;

namespace MangaManagementSystem.Web.Components.Pages.Mangaka;

public partial class MangakaDashboard
{
    private bool _newSeriesDialogOpen;
    private bool _draftCreated;
    private bool _draftSaving;

    private string _newDraftTitle = "";
    private string _newDraftSynopsis = "";
    private HashSet<Guid> _newDraftGenreIds = new();
    private HashSet<Guid> _newDraftTagIds = new();
    private string _newDraftLanguage = "ja";
    private string? _newDraftFrequency;
    private byte[]? _draftCoverBytes;
    private string? _draftCoverFileName;
    private string? _draftCoverContentType;
    private string? _draftCoverPreviewUrl; // cropped data: URI for create modal preview

    // â”€â”€ Edit Draft modal state (BF-SERIES-002) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private bool _draftDetailsOpen;
    private SeriesCardData? _detailSeries;

    // Editable form fields â€” populated when the modal opens
    private string _editTitle = "";
    private string _editSynopsis = "";
    private HashSet<Guid> _editGenreIds = new();
    private HashSet<Guid> _editTagIds = new();
    private string _editLanguage = "ja";
    private string? _editFrequency;
    private bool _draftSavingEdit;

    // Cover replacement (optional, PROPOSAL_DRAFT only)
    private byte[]? _editCoverBytes;
    private string? _editCoverFileName;
    private string? _editCoverContentType;
    private string? _editCoverPreviewUrl; // data: URI for immediate preview

    // â”€â”€ Cover crop dialog state â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€”€
    private bool _showCoverCropDialog;
    private string? _coverCropSourceDataUrl;
    private string? _coverCropSourceFileName;
    private bool _coverCropIsForCreate; // true = update create fields, false = update edit fields

    // â”€â”€ Cover preview popup state â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private bool _showCoverPreviewDialog;
    private string? _coverPreviewUrl;

    // â”€â”€ Review Status modal state â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private bool _reviewStatusOpen;
    private SeriesCardData? _reviewSeries;

    // â”€â”€ Dashboard modal state â”€â”€
    private bool IsAnyDashboardModalOpen =>
        _cancelDraftDialogOpen || _newSeriesDialogOpen || _draftDetailsOpen || _reviewStatusOpen || _submitProposalDialogOpen || _genrePickerOpen || _tagPickerOpen || _showCoverCropDialog || _showCoverPreviewDialog;

    // â”€â”€ Dashboard pagination â”€â”€
    private int _dashboardPage = 1;
    private const int DashboardPageSize = 8;

    private int DashboardTotalPages => DashboardPageSize == 0
        ? 0
        : (int)Math.Ceiling((double)AllFilteredSeries.Count / DashboardPageSize);

    private List<SeriesCardData> PagedSeriesData =>
        AllFilteredSeries
            .Skip((_dashboardPage - 1) * DashboardPageSize)
            .Take(DashboardPageSize)
            .ToList();

    private void ResetDashboardPage() => _dashboardPage = 1;

    private Guid? _currentUserId;
    private Guid? _openSeriesActionMenuId;

    private async Task LoadReferenceDataAsync()
    {
        try
        {
            _availableGenres = await ReferenceDataApiClient.GetGenresAsync();
            _availableTags = await ReferenceDataApiClient.GetTagsAsync();
        }
        catch
        {
            _availableGenres = Array.Empty<GenreDto>();
            _availableTags = Array.Empty<TagDto>();
        }
    }

    // â”€â”€ Genre Picker state â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private bool _genrePickerOpen;
    private bool _genrePickerForEdit;
    private string _genrePickerSearch = "";
    private int _genrePickerPage = 1;
    private const int GenrePickerPageSize = 8;
    private HashSet<Guid> _genrePickerSelection = new();

    private IReadOnlyList<GenreDto> FilteredPickerGenres
    {
        get
        {
            var search = _genrePickerSearch?.Trim() ?? "";
            IEnumerable<GenreDto> result = _availableGenres;
            if (!string.IsNullOrWhiteSpace(search))
                result = result.Where(g =>
                    g.GenreName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (g.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
            return result.OrderBy(g => g.GenreName).ToList();
        }
    }

    private int GenrePickerTotalPages => GenrePickerPageSize == 0
        ? 0
        : (int)Math.Ceiling((double)FilteredPickerGenres.Count / GenrePickerPageSize);

    private List<GenreDto> PagedPickerGenres =>
        FilteredPickerGenres
            .Skip((_genrePickerPage - 1) * GenrePickerPageSize)
            .Take(GenrePickerPageSize)
            .ToList();

    private void ResetGenrePickerPage() => _genrePickerPage = 1;

    private void OpenGenrePicker(bool forEdit)
    {
        _genrePickerForEdit = forEdit;
        _genrePickerSearch = "";
        _genrePickerPage = 1;
        _genrePickerSelection = new HashSet<Guid>(forEdit ? _editGenreIds : _newDraftGenreIds);
        _genrePickerOpen = true;
    }

    private void TogglePickerGenre(Guid id)
    {
        if (_genrePickerSelection.Count <= 1 && _genrePickerSelection.Contains(id))
            return;
        if (!_genrePickerSelection.Remove(id))
            _genrePickerSelection.Add(id);
    }

    private void ApplyGenrePicker()
    {
        if (_genrePickerSelection.Count == 0) return;
        var target = _genrePickerForEdit ? _editGenreIds : _newDraftGenreIds;
        target.Clear();
        foreach (var id in _genrePickerSelection) target.Add(id);
        _genrePickerOpen = false;
    }

    private void CloseGenrePicker()
    {
        _genrePickerOpen = false;
    }

    private void RemoveNewDraftGenre(Guid genreId)
    {
        if (_newDraftGenreIds.Count <= 1)
        {
            Snackbar.Add("At least one genre is required.", Severity.Warning);
            return;
        }
        _newDraftGenreIds.Remove(genreId);
    }

    private void RemoveEditGenre(Guid genreId)
    {
        if (_editGenreIds.Count <= 1)
        {
            Snackbar.Add("At least one genre is required.", Severity.Warning);
            return;
        }
        _editGenreIds.Remove(genreId);
    }

    // â”€â”€ Tag Picker state â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private bool _tagPickerOpen;
    private bool _tagPickerForEdit;
    private string _tagPickerSearch = "";
    private int _tagPickerPage = 1;
    private const int TagPickerPageSize = 10;
    private HashSet<Guid> _tagPickerSelection = new();

    private IReadOnlyList<TagDto> FilteredPickerTags
    {
        get
        {
            var search = _tagPickerSearch?.Trim() ?? "";
            IEnumerable<TagDto> result = _availableTags;
            if (!string.IsNullOrWhiteSpace(search))
                result = result.Where(t =>
                    t.TagName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    (t.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
            return result.OrderBy(t => t.TagName).ToList();
        }
    }

    private int TagPickerTotalPages => TagPickerPageSize == 0
        ? 0
        : (int)Math.Ceiling((double)FilteredPickerTags.Count / TagPickerPageSize);

    private List<TagDto> PagedPickerTags =>
        FilteredPickerTags
            .Skip((_tagPickerPage - 1) * TagPickerPageSize)
            .Take(TagPickerPageSize)
            .ToList();

    private void ResetTagPickerPage() => _tagPickerPage = 1;

    private void OpenTagPicker(bool forEdit)
    {
        _tagPickerForEdit = forEdit;
        _tagPickerSearch = "";
        _tagPickerPage = 1;
        _tagPickerSelection = new HashSet<Guid>(forEdit ? _editTagIds : _newDraftTagIds);
        _tagPickerOpen = true;
    }

    private void TogglePickerTag(Guid id)
    {
        if (!_tagPickerSelection.Remove(id))
            _tagPickerSelection.Add(id);
    }

    private void ApplyTagPicker()
    {
        var target = _tagPickerForEdit ? _editTagIds : _newDraftTagIds;
        target.Clear();
        foreach (var id in _tagPickerSelection) target.Add(id);
        _tagPickerOpen = false;
    }

    private void CloseTagPicker()
    {
        _tagPickerOpen = false;
    }

    private void RemoveNewDraftTag(Guid tagId)
    {
        _newDraftTagIds.Remove(tagId);
    }

    private void RemoveEditTag(Guid tagId)
    {
        _editTagIds.Remove(tagId);
    }

    // â”€â”€ Cancel Draft state â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private bool _cancelDraftDialogOpen;
    private Guid _cancelDraftSeriesId;
    private string? _cancelDraftSeriesTitle;
    private string? _cancelDraftReason;
    private bool _cancelDraftBusy;

    private void OpenCancelDraft(Guid seriesId, string title)
    {
        _openSeriesActionMenuId = null;
        _cancelDraftSeriesId = seriesId;
        _cancelDraftSeriesTitle = title;
        _cancelDraftReason = null;
        _cancelDraftBusy = false;
        _cancelDraftDialogOpen = true;
    }

    private void CloseCancelDraft()
    {
        _cancelDraftDialogOpen = false;
    }

    private async Task ConfirmCancelDraftAsync()
    {
        if (_currentUserId is null || _currentUserId == Guid.Empty)
        {
            Snackbar.Add("Could not identify the signed-in user. Please sign in again.", Severity.Error);
            return;
        }

        _cancelDraftBusy = true;
        try
        {
            await MangakaSeriesApiClient.CancelDraftAsync(
                actorUserId: _currentUserId.Value,
                seriesId: _cancelDraftSeriesId,
                reason: string.IsNullOrWhiteSpace(_cancelDraftReason) ? null : _cancelDraftReason.Trim(),
                cancellationToken: default);

            // Update card status in-memory. Edit Draft and Submit Proposal actions
            // disappear automatically because they are guarded by StatusCode == "PROPOSAL_DRAFT".
            var idx = _seriesData.FindIndex(s => s.Id == _cancelDraftSeriesId);
            if (idx >= 0)
            {
                _seriesData[idx] = _seriesData[idx] with { StatusCode = "CANCELLED" };
            }

            Snackbar.Add("Draft cancelled.", Severity.Success);
            _cancelDraftDialogOpen = false;
        }
        catch (Exception ex)
        {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally
        {
            _cancelDraftBusy = false;
        }
    }

    // â”€â”€ Submit Proposal state (BF-SERIES-003) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private bool _submitProposalDialogOpen;
    private Guid _submitProposalSeriesId;
    private string? _submitProposalSeriesTitle;
    private byte[]? _proposalFileBytes;
    private string? _proposalFileName;
    private string? _proposalContentType;
    private bool _proposalSubmitting;

    // Allowed proposal document extensions and MIME types (enforced in UI for fast feedback;
    // backend also validates in SubmitSeriesProposalCommandHandler).
    private static readonly HashSet<string> AllowedProposalExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx"
    };

    private static readonly HashSet<string> AllowedProposalContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    private const long MaxProposalFileSizeBytes = 10 * 1024 * 1024; // 10 MB

    // â”€â”€ Navigation dispatcher â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    /// <summary>
    /// Routes card clicks to the correct behaviour based on status.
    /// PROPOSAL_DRAFT â†’ Draft Details modal (not workspace).
    /// UNDER_EDITORIAL_REVIEW / UNDER_BOARD_REVIEW â†’ Review Status modal.
    /// SERIALIZED â†’ navigate to /series/{slug}.
    /// HIATUS / CANCELLED / COMPLETED â†’ Review Status modal (read-only).
    /// </summary>
    private void OpenSeriesCard(SeriesCardData series)
    {
        _openSeriesActionMenuId = null;
        switch (series.StatusCode)
        {
            case "PROPOSAL_DRAFT":
                OpenDraftDetails(series);
                break;
            case "SERIALIZED":
                if (!string.IsNullOrWhiteSpace(series.Slug))
                    Nav.NavigateTo($"/series/{series.Slug}?returnUrl={Uri.EscapeDataString("/mangaka")}");
                else
                    Snackbar.Add("Series page is not available yet.", Severity.Info);
                break;
            default:
                OpenReviewStatus(series);
                break;
        }
    }

    private void OpenDraftDetails(SeriesCardData series)
    {
        _openSeriesActionMenuId = null;
        _detailSeries = series;
        _editTitle = series.Title;
        _editSynopsis = series.Synopsis ?? "";
        _editGenreIds = new HashSet<Guid>(series.Genres.Select(g => g.GenreId));
        _editTagIds = new HashSet<Guid>(series.Tags.Select(t => t.TagId));
        _editLanguage = series.ContentLanguageCode ?? "ja";
        _editFrequency = series.PublicationFrequencyCode;
        _draftSavingEdit = false;
        ClearEditCover();
        _draftDetailsOpen = true;
    }

    private void ClearEditCover()
    {
        _editCoverBytes = null;
        _editCoverFileName = null;
        _editCoverContentType = null;
        _editCoverPreviewUrl = null;
    }

    private void OpenCoverPreview()
    {
        var url = _editCoverPreviewUrl
                  ?? _draftCoverPreviewUrl
                  ?? _detailSeries?.CoverUrl;

        if (string.IsNullOrWhiteSpace(url)) return;

        _coverPreviewUrl = url;
        _showCoverPreviewDialog = true;
    }

    private void CloseCoverPreview()
    {
        _showCoverPreviewDialog = false;
        _coverPreviewUrl = null;
    }

    /// <summary>
    /// Pre-read IBrowserFile bytes immediately for cover replacement.
    /// Validates image type (PNG/JPG/WEBP) and size (max 5 MB).
    /// Generates a data: URI preview so the user can verify the selection before saving.
    /// Does NOT call IFileStorageService â€” that happens in UpdateSeriesDraftCommandHandler via API.
    /// </summary>
    private async Task OnEditCoverChanged(IBrowserFile file)
    {
        if (file == null) return;

        const long maxCoverBytes = 5 * 1024 * 1024; // 5 MB

        var allowedCoverTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/png", "image/jpeg", "image/webp"
        };

        var allowedCoverExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".webp"
        };

        var ext = Path.GetExtension(file.Name);
        if (!allowedCoverExtensions.Contains(ext))
        {
            Snackbar.Add("Only PNG, JPG, and WEBP images are accepted as series covers.", Severity.Warning);
            return;
        }

        if (!allowedCoverTypes.Contains(file.ContentType))
        {
            Snackbar.Add("The selected file type is not accepted. Please upload a PNG, JPG, or WEBP image.", Severity.Warning);
            return;
        }

        if (file.Size > maxCoverBytes)
        {
            Snackbar.Add("The cover image exceeds the maximum size of 5 MB.", Severity.Warning);
            return;
        }

        try
        {
            using var stream = file.OpenReadStream(maxCoverBytes);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var bytes = ms.ToArray();

            _coverCropSourceDataUrl = $"data:{file.ContentType};base64,{Convert.ToBase64String(bytes)}";
            _coverCropSourceFileName = file.Name;
            _coverCropIsForCreate = false;
            _showCoverCropDialog = true;
        }
        catch (Exception)
        {
            Snackbar.Add("Failed to read the selected cover image. Please try again.", Severity.Error);
        }
    }

    private async Task OnCreateCoverChanged(IBrowserFile file)
    {
        if (file == null) return;

        const long maxCoverBytes = 5 * 1024 * 1024;

        var allowedCoverTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/png", "image/jpeg", "image/webp"
        };

        var allowedCoverExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".webp"
        };

        var ext = Path.GetExtension(file.Name);
        if (!allowedCoverExtensions.Contains(ext))
        {
            Snackbar.Add("Only PNG, JPG, and WEBP images are accepted as series covers.", Severity.Warning);
            return;
        }

        if (!allowedCoverTypes.Contains(file.ContentType))
        {
            Snackbar.Add("The selected file type is not accepted. Please upload a PNG, JPG, or WEBP image.", Severity.Warning);
            return;
        }

        if (file.Size > maxCoverBytes)
        {
            Snackbar.Add("The cover image exceeds the maximum size of 5 MB.", Severity.Warning);
            return;
        }

        try
        {
            using var stream = file.OpenReadStream(maxCoverBytes);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var bytes = ms.ToArray();

            _coverCropSourceDataUrl = $"data:{file.ContentType};base64,{Convert.ToBase64String(bytes)}";
            _coverCropSourceFileName = file.Name;
            _coverCropIsForCreate = true;
            _showCoverCropDialog = true;
        }
        catch (Exception)
        {
            Snackbar.Add("Failed to read the selected cover image. Please try again.", Severity.Error);
        }
    }

    private async Task OnCoverCropConfirmed((byte[] Bytes, string FileName, string ContentType) result)
    {
        _showCoverCropDialog = false;
        _coverCropSourceDataUrl = null;
        _coverCropSourceFileName = null;

        if (_coverCropIsForCreate)
        {
            _draftCoverBytes = result.Bytes;
            _draftCoverFileName = result.FileName;
            _draftCoverContentType = result.ContentType;
            _draftCoverPreviewUrl = $"data:{result.ContentType};base64,{Convert.ToBase64String(result.Bytes)}";
        }
        else
        {
            _editCoverBytes = result.Bytes;
            _editCoverFileName = result.FileName;
            _editCoverContentType = result.ContentType;
            _editCoverPreviewUrl = $"data:{result.ContentType};base64,{Convert.ToBase64String(result.Bytes)}";
        }
    }

    private async Task OnCoverCropCancelled()
    {
        _showCoverCropDialog = false;
        _coverCropSourceDataUrl = null;
        _coverCropSourceFileName = null;
    }

    private void ClearCreateCover()
    {
        _draftCoverBytes = null;
        _draftCoverFileName = null;
        _draftCoverContentType = null;
        _draftCoverPreviewUrl = null;
    }

    private async Task SaveDraftEditAsync()
    {
        if (string.IsNullOrWhiteSpace(_editTitle) || _editGenreIds.Count == 0) return;

        if (string.IsNullOrWhiteSpace(_editSynopsis))
        {
            Snackbar.Add("Synopsis / Description is required.", Severity.Warning);
            return;
        }

        if (_currentUserId is null || _currentUserId == Guid.Empty)
        {
            Snackbar.Add("Could not identify the signed-in user. Please sign in again.", Severity.Error);
            return;
        }

        if (_detailSeries == null) return;

        _draftSavingEdit = true;
        try
        {
            var result = await MangakaSeriesApiClient.UpdateDraftAsync(
                actorUserId: _currentUserId.Value,
                seriesId: _detailSeries.Id,
                title: _editTitle.Trim(),
                synopsis: _editSynopsis.Trim(),
                genreIds: _editGenreIds.ToList(),
                tagIds: _editTagIds.ToList(),
                contentLanguageCode: _editLanguage,
                publicationFrequencyCode: _editFrequency,
                slug: null, // auto-derived from title in the handler
                coverFileBytes: _editCoverBytes,
                coverFileName: _editCoverFileName,
                coverContentType: _editCoverContentType,
                cancellationToken: default);

            // Update succeeded â€” now refresh this single card from the API.
            var idx = _seriesData.FindIndex(s => s.Id == _detailSeries.Id);
            if (idx >= 0)
            {
                try
                {
                    var freshDto = await MangakaSeriesApiClient.GetMySeriesCardByIdAsync(
                        _currentUserId.Value, _detailSeries.Id);

                    if (freshDto is not null)
                    {
                        var fresh = new SeriesCardData(
                            Id: freshDto.SeriesId,
                            Title: freshDto.Title,
                            GenreDisplay: string.Join(", ", freshDto.Genres.Select(g => g.GenreName)),
                            Genres: freshDto.Genres,
                            Tags: freshDto.Tags,
                            StatusCode: freshDto.StatusCode,
                            Slug: freshDto.Slug,
                            Synopsis: freshDto.Synopsis,
                            ContentLanguageCode: freshDto.ContentLanguageCode,
                            CoverUrl: freshDto.CoverUrl,
                            CreatedAtUtc: freshDto.CreatedAtUtc,
                            UpdatedAtUtc: freshDto.UpdatedAtUtc,
                            PublicationFrequencyCode: freshDto.PublicationFrequencyCode
                        );
                        _seriesData[idx] = fresh;
                        _detailSeries = fresh;
                        Snackbar.Add("Draft profile saved.", Severity.Success);
                    }
                    else
                    {
                        // Server returned 404 â€” this series is no longer available to this user.
                        // Do NOT fabricate a fake card from command result + local selections.
                        _seriesData.RemoveAt(idx);
                        _detailSeries = null;
                        Snackbar.Add(
                            "The draft was saved, but this series is no longer available in your dashboard. Please refresh if needed.",
                            Severity.Warning);
                    }
                }
                catch (Exception)
                {
                    // Targeted card refresh failed (network/server error), but the update did succeed.
                    // Keep the existing card unchanged â€” it may be stale but is safer than fabricating data.
                    Snackbar.Add(
                        "The draft was saved, but the card could not be refreshed. Please reload the page.",
                        Severity.Warning);
                }
            }

            _draftDetailsOpen = false;
        }
        catch (Exception ex)
        {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally
        {
            _draftSavingEdit = false;
        }
    }

    private void OpenReviewStatus(SeriesCardData series)
    {
        _openSeriesActionMenuId = null;
        _reviewSeries = series;
        _reviewStatusOpen = true;
    }

    private void OpenSubmitProposal(Guid seriesId, string title)
    {
        _openSeriesActionMenuId = null;
        _submitProposalSeriesId = seriesId;
        _submitProposalSeriesTitle = title;
        _proposalFileBytes = null;
        _proposalFileName = null;
        _proposalContentType = null;
        _proposalSubmitting = false;
        _submitProposalDialogOpen = true;
    }

    private void CloseSubmitProposal()
    {
        _submitProposalDialogOpen = false;
    }

    private void ClearProposalFile()
    {
        _proposalFileBytes = null;
        _proposalFileName = null;
        _proposalContentType = null;
    }

    /// <summary>
    /// Pre-read IBrowserFile bytes immediately â€” do not hold IBrowserFile across re-renders.
    /// Validates extension, content type, and size before storing in component state.
    /// </summary>
    private async Task OnProposalFileChanged(IBrowserFile file)
    {
        if (file == null) return;

        var ext = Path.GetExtension(file.Name);
        if (!AllowedProposalExtensions.Contains(ext))
        {
            Snackbar.Add("Only PDF, DOC, and DOCX files are accepted as proposal documents.", Severity.Warning);
            return;
        }

        if (!AllowedProposalContentTypes.Contains(file.ContentType))
        {
            Snackbar.Add("The selected file type is not accepted. Please upload a PDF, DOC, or DOCX document.", Severity.Warning);
            return;
        }

        if (file.Size > MaxProposalFileSizeBytes)
        {
            Snackbar.Add("The proposal file exceeds the maximum allowed size of 10 MB.", Severity.Warning);
            return;
        }

        try
        {
            using var stream = file.OpenReadStream(MaxProposalFileSizeBytes);
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            _proposalFileBytes = ms.ToArray();
            _proposalFileName = file.Name;
            _proposalContentType = file.ContentType;
        }
        catch (Exception)
        {
            Snackbar.Add("Failed to read the selected file. Please try again.", Severity.Error);
            ClearProposalFile();
        }
    }

    private async Task SubmitProposalAsync()
    {
        if (_proposalFileBytes == null || string.IsNullOrWhiteSpace(_proposalFileName))
        {
            Snackbar.Add("Please select a proposal document before submitting.", Severity.Warning);
            return;
        }

        if (_currentUserId is null || _currentUserId == Guid.Empty)
        {
            Snackbar.Add("Could not identify the signed-in user. Please sign in again.", Severity.Error);
            return;
        }

        _proposalSubmitting = true;
        try
        {
            var result = await MangakaSeriesApiClient.SubmitProposalAsync(
                actorUserId: _currentUserId.Value,
                seriesId: _submitProposalSeriesId,
                proposalFileBytes: _proposalFileBytes,
                proposalFileName: _proposalFileName,
                proposalContentType: _proposalContentType ?? "application/octet-stream",
                cancellationToken: default);

            // Update the affected card status in the in-memory list.
            var idx = _seriesData.FindIndex(s => s.Id == _submitProposalSeriesId);
            if (idx >= 0)
            {
                _seriesData[idx] = _seriesData[idx] with { StatusCode = "UNDER_EDITORIAL_REVIEW" };
            }

            Snackbar.Add(
                $"Proposal submitted (v{result.ProposalVersionNo}). Series is now Under Editorial Review.",
                Severity.Success);

            _submitProposalDialogOpen = false;
        }
        catch (Exception ex)
        {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally
        {
            _proposalSubmitting = false;
        }
    }

    private IReadOnlyList<GenreDto> _availableGenres = Array.Empty<GenreDto>();
    private IReadOnlyList<TagDto> _availableTags = Array.Empty<TagDto>();

    private readonly (string Code, string Label)[] _languages =
    [
        ("ja", "Japanese"),
        ("en", "English"),
        ("vi", "Vietnamese"),
    ];

    private List<SeriesCardData> _seriesData = [];

    // â”€â”€ Filter / search / sort state â”€â”€
    private string _selectedFilter = "All";
    private string _searchText = "";
    private string _sortMode = "recent"; // "recent" | "newest" | "title"
    private HashSet<Guid> _filterGenreIds = new();
    private HashSet<Guid> _filterTagIds = new();

    private static readonly (string Label, string[] Statuses)[] FilterGroups =
    [
        ("All",             []),
        ("Draft",           ["PROPOSAL_DRAFT"]),
        ("In Review",       ["UNDER_EDITORIAL_REVIEW", "UNDER_BOARD_REVIEW"]),
        ("Serialized",      ["SERIALIZED"]),
        ("Paused / Hiatus", ["HIATUS"]),
        ("Cancelled",       ["CANCELLED"]),
        ("Completed",       ["COMPLETED"]),
    ];

    private int FilterCount(string[] statuses)
        => statuses.Length == 0
            ? _seriesData.Count
            : _seriesData.Count(s => statuses.Contains(s.StatusCode));

    private List<SeriesCardData> AllFilteredSeries
    {
        get
        {
            var group = FilterGroups.FirstOrDefault(g => g.Label == _selectedFilter);
            IEnumerable<SeriesCardData> result = _seriesData;

            // Status filter
            if (group.Statuses is { Length: > 0 })
                result = result.Where(s => group.Statuses.Contains(s.StatusCode));

            // Title search only
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                var term = _searchText.Trim();
                result = result.Where(s =>
                    s.Title.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            // Genre filter â€” show series that contain ALL selected genres
            if (_filterGenreIds.Count > 0)
            {
                result = result.Where(s =>
                    _filterGenreIds.All(fgId => s.Genres.Any(g => g.GenreId == fgId)));
            }

            // Tag filter â€” show series that contain ALL selected tags
            if (_filterTagIds.Count > 0)
            {
                result = result.Where(s =>
                    _filterTagIds.All(ftId => s.Tags.Any(t => t.TagId == ftId)));
            }

            // Sort
            result = _sortMode switch
            {
                "newest" => result.OrderByDescending(s => s.CreatedAtUtc),
                "title" => result.OrderBy(s => s.Title, StringComparer.OrdinalIgnoreCase),
                _ => result.OrderByDescending(s => s.UpdatedAtUtc ?? s.CreatedAtUtc), // "recent"
            };

            return result.ToList();
        }
    }

    private string EmptyStateMessage
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(_searchText) || _filterGenreIds.Count > 0 || _filterTagIds.Count > 0)
                return "No series match your filters.";

            return _selectedFilter switch
            {
                "Draft" => "No draft series. Create a new draft to get started.",
                "In Review" => "No series currently under review.",
                "Serialized" => "No serialized series yet.",
                "Paused / Hiatus" => "No series on hiatus.",
                "Cancelled" => "No cancelled series.",
                "Completed" => "No completed series.",
                _ => "You don't have any series yet. Create a new draft to get started.",
            };
        }
    }

    /// <summary>
    /// Produces a client-side slug preview using the same SlugGenerator logic the backend
    /// uses. This is display-only â€” the server always generates the authoritative slug.
    /// </summary>
    private static string BuildSlugPreview(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

        try
        {
            return SlugGenerator.FromTitle(title);
        }
        catch
        {
            return string.Empty;
        }
    }

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            var idClaim = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(idClaim, out var uid))
            {
                _currentUserId = uid;
            }
        }
        catch { }

        await LoadSeriesAsync();
        await LoadReferenceDataAsync();
    }

    private async Task LoadSeriesAsync()
    {
        if (_currentUserId is null || _currentUserId == Guid.Empty)
        {
            _seriesData = [];
            return;
        }

        try
        {
            var dtos = await MangakaSeriesApiClient.GetMySeriesAsync(_currentUserId.Value);
            _seriesData = dtos.Select(s => new SeriesCardData(
                Id: s.SeriesId,
                Title: s.Title,
                GenreDisplay: string.Join(", ", s.Genres.Select(g => g.GenreName)),
                Genres: s.Genres,
                Tags: s.Tags,
                StatusCode: s.StatusCode,
                Slug: s.Slug,
                Synopsis: s.Synopsis,
                ContentLanguageCode: s.ContentLanguageCode,
                CoverUrl: s.CoverUrl,
                CreatedAtUtc: s.CreatedAtUtc,
                UpdatedAtUtc: s.UpdatedAtUtc,
                PublicationFrequencyCode: s.PublicationFrequencyCode
            )).ToList();
        }
        catch
        {
            _seriesData = [];
        }
    }

    private async Task CreateSeriesDraft()
    {
        if (string.IsNullOrWhiteSpace(_newDraftTitle) || _newDraftGenreIds.Count == 0) return;

        if (_currentUserId is null || _currentUserId == Guid.Empty)
        {
            Snackbar.Add("Could not identify the signed-in user. Please sign in again.", Severity.Error);
            return;
        }

        _draftSaving = true;
        try
        {
            // Flow: Dashboard -> typed Web API client -> API controller -> Application service
            // -> Infrastructure -> manga.usp_Series_Create. The stored procedure creates the
            // Series (PROPOSAL_DRAFT), the optional SERIES_COVER FileResource, the creator
            // contributor, and the audit event. No SeriesProposal is created here.
            var created = await MangakaSeriesApiClient.CreateDraftAsync(
                actorUserId: _currentUserId.Value,
                title: _newDraftTitle.Trim(),
                synopsis: string.IsNullOrWhiteSpace(_newDraftSynopsis) ? _newDraftTitle.Trim() : _newDraftSynopsis.Trim(),
                genreIds: _newDraftGenreIds.ToList(),
                tagIds: _newDraftTagIds.ToList(),
                contentLanguageCode: _newDraftLanguage,
                slug: null,
                publicationFrequencyCode: _newDraftFrequency,
                sourceSeriesId: null,
                coverFileBytes: _draftCoverBytes,
                coverFileName: _draftCoverFileName,
                coverContentType: _draftCoverContentType);

            _seriesData.Add(new SeriesCardData(
                Id: created.SeriesId,
                Title: created.Title,
                GenreDisplay: string.Join(", ", _availableGenres.Where(g => _newDraftGenreIds.Contains(g.GenreId)).Select(g => g.GenreName)),
                Genres: _availableGenres.Where(g => _newDraftGenreIds.Contains(g.GenreId)).ToList(),
                Tags: _availableTags.Where(t => _newDraftTagIds.Contains(t.TagId)).ToList(),
                StatusCode: created.StatusCode,
                Slug: created.Slug,
                Synopsis: string.IsNullOrWhiteSpace(_newDraftSynopsis) ? created.Title : _newDraftSynopsis.Trim(),
                ContentLanguageCode: _newDraftLanguage,
                CoverUrl: null, // cover URL requires a DB round-trip; will show on next full load
                CreatedAtUtc: DateTime.UtcNow,
                UpdatedAtUtc: null,
                PublicationFrequencyCode: _newDraftFrequency
            ));

            _draftCreated = true;
            Snackbar.Add($"Draft created. Status: {SeriesStatusHelper.DisplayName(created.StatusCode)}.", Severity.Success);

            await Task.Delay(1200);

            _draftCreated = false;
            _newDraftTitle = "";
            _newDraftSynopsis = "";
            _newDraftGenreIds.Clear();
            _newDraftTagIds.Clear();
            _newDraftLanguage = "ja";
            _newDraftFrequency = null;
            _draftCoverBytes = null;
            _draftCoverFileName = null;
            _draftCoverContentType = null;
            _draftCoverPreviewUrl = null;
            _newSeriesDialogOpen = false;
        }
        catch (Exception ex)
        {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally
        {
            _draftSaving = false;
        }
    }

    private record SeriesCardData(
        Guid Id,
        string Title,
        string GenreDisplay,
        IReadOnlyList<GenreDto> Genres,
        IReadOnlyList<TagDto> Tags,
        string StatusCode,
        string Slug,
        string? Synopsis,
        string? ContentLanguageCode,
        string? CoverUrl,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc,
        string? PublicationFrequencyCode)
    {
        public string Color => StatusCode switch
        {
            "PROPOSAL_DRAFT" => "#6366f1",
            "UNDER_EDITORIAL_REVIEW" => "#0ea5e9",
            "UNDER_BOARD_REVIEW" => "#8b5cf6",
            "SERIALIZED" => "#059669",
            "HIATUS" => "#d97706",
            "CANCELLED" => "#dc2626",
            "COMPLETED" => "#4f46e5",
            _ => "#64748b",
        };

        public string LastUpdated
        {
            get
            {
                var dt = UpdatedAtUtc ?? CreatedAtUtc;
                var ago = DateTime.UtcNow - dt;
                if (ago.TotalMinutes < 60) return $"{(int)ago.TotalMinutes} min{(ago.TotalMinutes < 2 ? "" : "s")} ago";
                if (ago.TotalHours < 24) return $"{(int)ago.TotalHours} hour{(ago.TotalHours < 2 ? "" : "s")} ago";
                return $"{(int)ago.TotalDays} day{(ago.TotalDays < 2 ? "" : "s")} ago";
            }
        }
    }
}
