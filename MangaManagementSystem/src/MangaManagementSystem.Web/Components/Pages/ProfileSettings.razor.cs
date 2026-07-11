using SixLabors.ImageSharp.Processing;
using System.Security.Claims;
using System.IO;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Globalization;
using Microsoft.JSInterop;
using MudBlazor;
using MangaManagementSystem.Application.DTOs.Auth;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Web.Services.Api;

namespace MangaManagementSystem.Web.Components.Pages;

public partial class ProfileSettings
{
    private const long MaxUploadBytes = 10 * 1024 * 1024;

    private const string ActionPasswordOtp = "PROFILE_PASSWORD_RESET";

    private const string AvatarCropCanvasId =
        "profile-settings-avatar-crop-canvas";

    private bool _isLoading = true;
    private bool _isBusy;

    private UserDto? _currentUser;
    private Guid _currentUserId;
    private string _currentRoleName = "User";

    private string _displayName = string.Empty;
    private string _avatarInitial = "?";
    private string? _avatarUrl;
    private string? _portfolioUrl;
    private string? _portfolioFileName;
    private string _portfolioTypeLabel = "FILE";

    private FileResourceDto? _currentPortfolio;
    private bool _showPortfolioPreview;
    private bool _isImagePreview;

    private readonly DialogOptions _portfolioPreviewDialogOptions =
        new()
        {
            FullWidth = true,
            MaxWidth = MaxWidth.ExtraLarge,
            CloseButton = true
        };

    private string? _operationMessage;
    private Severity _operationSeverity = Severity.Info;

    private IJSObjectReference? _avatarCropModule;

    private bool _showAvatarCropDialog;
    private bool _initializeAvatarCrop;
    private bool _avatarCropReady;
    private bool _avatarCropBusy;

    private string? _avatarCropSourceDataUrl;
    private string? _pendingAvatarOriginalFileName;

    private double _avatarCropZoom = 1;
    private int _avatarInputKey;

    private string? _avatarPreviewDataUrl;
    private byte[]? _selectedAvatarBytes;
    private string? _selectedAvatarFileName;
    private string? _selectedAvatarUploadFileName;
    private string? _selectedAvatarSizeLabel;
    private string _selectedAvatarContentType = "image/jpeg";

    private string? _selectedPortfolioFileName;
    private string? _selectedPortfolioUploadFileName;
    private string? _selectedPortfolioSizeLabel;
    private byte[]? _selectedPortfolioBytes;
    private string? _selectedPortfolioContentType;

    private string _newPassword = string.Empty;
    private string _confirmPassword = string.Empty;
    private bool _otpSentForPassword;
    private string _otpCodePassword = string.Empty;

    private ProfileSettingsSection _activeSection =
        ProfileSettingsSection.Profile;

    private string CurrentSectionTitle => _activeSection switch
    {
        ProfileSettingsSection.Profile => "Profile",
        ProfileSettingsSection.Portfolio => "Portfolio Document",
        ProfileSettingsSection.Security => "Security",
        _ => "Profile"
    };

    private string CurrentSectionDescription => _activeSection switch
    {
        ProfileSettingsSection.Profile =>
            "Update your display name and profile picture.",

        ProfileSettingsSection.Portfolio =>
            "Manage your PDF, DOC, or DOCX portfolio document.",

        ProfileSettingsSection.Security =>
            "Reset password with email OTP confirmation.",

        _ => string.Empty
    };

    protected override async Task OnInitializedAsync()
    {
        await LoadCurrentUserAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_showAvatarCropDialog ||
            !_initializeAvatarCrop ||
            string.IsNullOrWhiteSpace(
                _avatarCropSourceDataUrl))
        {
            return;
        }

        _initializeAvatarCrop = false;

        try
        {
            /*
             * Thêm version vào URL để trình duyệt không dùng
             * lại module JavaScript lỗi đã lưu trong cache.
             */
            _avatarCropModule ??=
                await JSRuntime.InvokeAsync<IJSObjectReference>(
                    "import",
                    "./js/profile-settings-crop.js?v=20260614-04");

            await _avatarCropModule.InvokeVoidAsync(
                "initialize",
                AvatarCropCanvasId,
                _avatarCropSourceDataUrl);

            _avatarCropReady = true;
        }
        catch (Exception ex)
        {
            _showAvatarCropDialog = false;
            _avatarCropReady = false;

            SetOperation(
                $"Unable to open avatar crop editor: {ex.Message}",
                Severity.Error);

            Snackbar.Add(
                $"Unable to open avatar crop editor: {ex.Message}",
                Severity.Error);
        }

        StateHasChanged();
    }
    private void BackToDashboard()
    {
        var normalizedRole =
            _currentRoleName?
                .Trim()
                .ToLowerInvariant()
            ?? string.Empty;

        var dashboardUrl = normalizedRole switch
        {
            "admin" => "/admin",

            "mangaka" => "/mangaka",

            "assistant" => "/assistant",

            "tantou editor" => "/editor",

            "editorial board member" => "/ranking",

            "editorial board chief" => "/ranking",

            _ => "/dashboard"
        };

        NavigationManager.NavigateTo(dashboardUrl);
    }
    private void SelectSection(ProfileSettingsSection section)
    {
        _activeSection = section;
        _operationMessage = null;
    }

    private string GetMenuClass(ProfileSettingsSection section)
    {
        return _activeSection == section
            ? "mf-menu-item active"
            : "mf-menu-item";
    }

    private async Task LoadCurrentUserAsync()
    {
        _isLoading = true;
        _operationMessage = null;

        try
        {
            var authState =
                await AuthStateProvider.GetAuthenticationStateAsync();

            var principal = authState.User;

            if (principal?.Identity?.IsAuthenticated != true)
            {
                NavigationManager.NavigateTo("/login");
                return;
            }

            var idClaim =
                principal.FindFirst(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(idClaim?.Value, out _currentUserId))
            {
                SetOperation(
                    "Unable to resolve current user id.",
                    Severity.Error);

                return;
            }

            _currentUser =
                await ProfileApiClient.GetProfileAsync(_currentUserId);

            if (_currentUser == null)
            {
                SetOperation(
                    "Unable to load current user profile.",
                    Severity.Error);

                return;
            }

            _currentRoleName =
                principal.FindFirst(ClaimTypes.Role)?.Value
                ?? principal.FindFirst("role")?.Value
                ?? _currentUser.RoleName
                ?? "User";

            _displayName =
                _currentUser.DisplayName;

            _avatarInitial =
                BuildInitial(
                    _currentUser.DisplayName,
                    _currentUser.Username);

            await LoadCurrentFilesAsync();
        }
        catch (Exception ex)
        {
            SetOperation(ex.Message, Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task LoadCurrentFilesAsync()
    {
        _avatarUrl = null;
        _portfolioUrl = null;
        _portfolioFileName = null;
        _portfolioTypeLabel = "FILE";

        if (_currentUser?.AvatarFileId.HasValue == true)
        {
            var avatarFile =
                await ProfileApiClient.GetFileAsync(
                    _currentUser.AvatarFileId.Value);

            if (avatarFile != null &&
                avatarFile.DeletedAtUtc == null &&
                !string.IsNullOrWhiteSpace(
                    avatarFile.CloudinarySecureUrl))
            {
                _avatarUrl =
                    BuildVersionedUrl(
                        avatarFile.CloudinarySecureUrl,
                        avatarFile.FileResourceId);
            }
        }

        if (_currentUser?.PortfolioFileId.HasValue == true)
        {
            var portfolioFile =
                await ProfileApiClient.GetFileAsync(
                    _currentUser.PortfolioFileId.Value);

            if (portfolioFile != null &&
                portfolioFile.DeletedAtUtc == null &&
                !string.IsNullOrWhiteSpace(
                    portfolioFile.CloudinarySecureUrl))
            {
                _portfolioUrl =
                    BuildVersionedUrl(
                        portfolioFile.CloudinarySecureUrl,
                        portfolioFile.FileResourceId);

                _portfolioFileName =
                    portfolioFile.OriginalFileName;

                _portfolioTypeLabel =
                    BuildDocumentTypeLabel(
                        portfolioFile.OriginalFileName,
                        portfolioFile.ContentType);
            }
        }
    }

    private async Task OpenCurrentPortfolioPreviewAsync()
    {
        if (_currentUser?.PortfolioFileId is not Guid fileResourceId)
        {
            Snackbar.Add(
                "No portfolio document is linked to this account.",
                Severity.Warning);

            return;
        }

        try
        {
            var file =
                await ProfileApiClient.GetFileAsync(
                    fileResourceId);

            if (file == null || file.DeletedAtUtc != null)
            {
                Snackbar.Add(
                    "Portfolio file was not found or is unavailable.",
                    Severity.Error);

                return;
            }

            _currentPortfolio = file;

            _isImagePreview =
                file.ContentType?.StartsWith(
                    "image/",
                    StringComparison.OrdinalIgnoreCase) == true;

            _showPortfolioPreview = true;
        }
        catch (Exception ex)
        {
            Snackbar.Add(
                $"Failed to load portfolio: {ex.Message}",
                Severity.Error);
        }
    }

    private async Task UpdateDisplayNameDirectAsync()
    {
        if (_currentUser == null)
        {
            return;
        }

        var trimmed = _displayName.Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            SetOperation(
                "Display name cannot be empty.",
                Severity.Warning);

            return;
        }

        if (string.Equals(
                trimmed,
                _currentUser.DisplayName,
                StringComparison.Ordinal))
        {
            SetOperation(
                "Display name is unchanged.",
                Severity.Info);

            return;
        }

        await RunBusyAsync(async () =>
        {
            var updated =
                await ProfileApiClient.UpdateDisplayNameAsync(
                    _currentUserId,
                    trimmed);

            _currentUser = updated;
            _displayName = updated.DisplayName;

            _avatarInitial =
                BuildInitial(
                    updated.DisplayName,
                    updated.Username);

            SetOperation(
                "Display name updated successfully.",
                Severity.Success);

            Snackbar.Add(
                "Display name updated successfully.",
                Severity.Success);
        });
    }

    private async Task SelectAvatarAsync(
        InputFileChangeEventArgs args)
    {
        var file = args.File;

        if (file == null)
        {
            return;
        }

        if (!IsSupportedAvatarFile(
                file.Name,
                file.ContentType))
        {
            SetOperation(
                "Avatar must be PNG, JPG, JPEG, or WEBP.",
                Severity.Warning);

            return;
        }

        if (file.Size > MaxUploadBytes)
        {
            SetOperation(
                "Avatar file must be 10MB or smaller.",
                Severity.Warning);

            return;
        }

        await RunBusyAsync(async () =>
        {
            var bytes =
                await ReadFileBytesAsync(file);

            var normalizedContentType =
                NormalizeAvatarContentType(
                    file.Name,
                    file.ContentType);

            _pendingAvatarOriginalFileName =
                Path.GetFileName(file.Name);

            // Resize to max 512px to avoid sending large base64 over SignalR
            var resizedBytes = await ResizeImageForPreviewAsync(bytes, normalizedContentType);

            _avatarCropSourceDataUrl =
                BuildDataUrl(
                    normalizedContentType,
                    resizedBytes);

            _selectedAvatarBytes = null;
            _selectedAvatarFileName = null;
            _selectedAvatarUploadFileName = null;
            _selectedAvatarSizeLabel = null;
            _avatarPreviewDataUrl = null;

            _avatarCropZoom = 1;
            _avatarCropReady = false;
            _avatarCropBusy = false;
            _showAvatarCropDialog = true;
            _initializeAvatarCrop = true;

            SetOperation(
                "Adjust the avatar position and zoom, then choose Use This Image.",
                Severity.Info);
        });
    }

    private async Task AvatarCropZoomChangedAsync(
        ChangeEventArgs args)
    {
        if (!_avatarCropReady ||
            _avatarCropModule == null)
        {
            return;
        }

        if (!double.TryParse(
                args.Value?.ToString(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var zoom))
        {
            return;
        }

        await SetAvatarCropZoomAsync(zoom);
    }

    private async Task ZoomAvatarOutAsync()
    {
        await SetAvatarCropZoomAsync(
            _avatarCropZoom - 0.1);
    }

    private async Task ZoomAvatarInAsync()
    {
        await SetAvatarCropZoomAsync(
            _avatarCropZoom + 0.1);
    }

    private async Task SetAvatarCropZoomAsync(double zoom)
    {
        if (!_avatarCropReady ||
            _avatarCropModule == null)
        {
            return;
        }

        _avatarCropZoom =
            Math.Clamp(zoom, 1, 3);

        await _avatarCropModule.InvokeVoidAsync(
            "setZoom",
            AvatarCropCanvasId,
            _avatarCropZoom);
    }

    private async Task ResetAvatarCropAsync()
    {
        if (!_avatarCropReady ||
            _avatarCropModule == null)
        {
            return;
        }

        _avatarCropZoom = 1;

        await _avatarCropModule.InvokeVoidAsync(
            "reset",
            AvatarCropCanvasId);
    }

    private async Task ConfirmAvatarCropAsync()
    {
        if (!_avatarCropReady ||
            _avatarCropModule == null ||
            _avatarCropBusy)
        {
            return;
        }

        _avatarCropBusy = true;

        try
        {
            await using var croppedStreamReference =
                await _avatarCropModule.InvokeAsync<IJSStreamReference>(
                    "exportCroppedImageStream",
                    AvatarCropCanvasId,
                    512);

            await using var croppedStream =
                await croppedStreamReference.OpenReadStreamAsync(
                    maxAllowedSize: MaxUploadBytes);

            using var memoryStream =
                new MemoryStream();

            await croppedStream.CopyToAsync(
                memoryStream);

            var croppedBytes =
                memoryStream.ToArray();

            if (croppedBytes.Length == 0)
            {
                throw new InvalidOperationException(
                    "The cropped avatar image is empty.");
            }

            var originalName =
                string.IsNullOrWhiteSpace(
                    _pendingAvatarOriginalFileName)
                    ? "avatar"
                    : Path.GetFileNameWithoutExtension(
                        _pendingAvatarOriginalFileName);

            var croppedFileName =
                $"{originalName}_cropped.png";

            _selectedAvatarBytes =
                croppedBytes;

            _selectedAvatarFileName =
                croppedFileName;

            _selectedAvatarUploadFileName =
                BuildUniqueStoredFileName(
                    croppedFileName,
                    "avatar");

            _selectedAvatarSizeLabel =
                FormatFileSize(
                    croppedBytes.LongLength);

            _selectedAvatarContentType =
                "image/png";

            _avatarPreviewDataUrl =
                BuildDataUrl(
                    "image/png",
                    croppedBytes);

            await CloseAvatarCropDialogAsync();

            _avatarInputKey++;

            SetOperation(
                "Avatar crop completed. Upload when ready.",
                Severity.Success);
        }
        catch (Exception ex)
        {
            var message =
                $"Unable to crop avatar: {ex.Message}";

            SetOperation(
                message,
                Severity.Error);

            Snackbar.Add(
                message,
                Severity.Error);
        }
        finally
        {
            _avatarCropBusy = false;
        }
    }
    private async Task CancelAvatarCropAsync()
    {
        if (_avatarCropBusy)
        {
            return;
        }

        await CloseAvatarCropDialogAsync();

        ClearAvatarSelection();

        SetOperation(
            "Avatar selection canceled.",
            Severity.Info);
    }

    private async Task CloseAvatarCropDialogAsync()
    {
        if (_avatarCropModule != null &&
            _avatarCropReady)
        {
            try
            {
                await _avatarCropModule.InvokeVoidAsync(
                    "dispose",
                    AvatarCropCanvasId);
            }
            catch (JSDisconnectedException)
            {
                // Browser connection already closed.
            }
        }

        _showAvatarCropDialog = false;
        _initializeAvatarCrop = false;
        _avatarCropReady = false;
        _avatarCropSourceDataUrl = null;
        _pendingAvatarOriginalFileName = null;
        _avatarCropZoom = 1;

        _avatarInputKey++;
    }

    private static byte[] ConvertDataUrlToBytes(
        string dataUrl)
    {
        if (string.IsNullOrWhiteSpace(dataUrl))
        {
            throw new InvalidOperationException(
                "The cropped avatar data is empty.");
        }

        var commaIndex =
            dataUrl.IndexOf(',');

        if (commaIndex < 0 ||
            commaIndex == dataUrl.Length - 1)
        {
            throw new InvalidOperationException(
                "The cropped avatar data is invalid.");
        }

        var base64Data =
            dataUrl[(commaIndex + 1)..];

        return Convert.FromBase64String(
            base64Data);
    }

    private async Task UploadAvatarDirectAsync()
    {
        if (_selectedAvatarBytes == null ||
            string.IsNullOrWhiteSpace(
                _selectedAvatarUploadFileName) ||
            string.IsNullOrWhiteSpace(
                _selectedAvatarFileName))
        {
            SetOperation(
                "Please select and crop an avatar image first.",
                Severity.Warning);

            return;
        }

        await RunBusyAsync(async () =>
        {
            var displayFileName =
                _selectedAvatarFileName!;

            var updated =
                await ProfileApiClient.UpdateAvatarAsync(
                    _currentUserId,
                    _selectedAvatarBytes,
                    displayFileName,
                    _selectedAvatarContentType);

            var newAvatarFileId =
                updated.AvatarFileId
                ?? throw new InvalidOperationException(
                    "The updated avatar FileResource id was not returned.");

            var avatarFile =
                await ProfileApiClient.GetFileAsync(
                    newAvatarFileId)
                ?? throw new InvalidOperationException(
                    "The uploaded avatar file could not be loaded.");

            _currentUser = updated;

            _avatarUrl =
                BuildVersionedUrl(
                    avatarFile.CloudinarySecureUrl,
                    newAvatarFileId);

            ClearAvatarSelection();

            SetOperation(
                "Avatar uploaded successfully.",
                Severity.Success);

            Snackbar.Add(
                "Avatar uploaded successfully.",
                Severity.Success);
        });
    }

    private async Task SelectPortfolioAsync(
        InputFileChangeEventArgs args)
    {
        var file = args.File;

        if (file == null)
        {
            return;
        }

        if (!IsSupportedPortfolioFile(
                file.Name,
                file.ContentType))
        {
            SetOperation(
                "Portfolio must be PDF, DOC, or DOCX.",
                Severity.Warning);

            return;
        }

        if (file.Size > MaxUploadBytes)
        {
            SetOperation(
                "Portfolio file must be 10MB or smaller.",
                Severity.Warning);

            return;
        }

        await RunBusyAsync(async () =>
        {
            var bytes =
                await ReadFileBytesAsync(file);

            _selectedPortfolioFileName =
                Path.GetFileName(file.Name);

            _selectedPortfolioUploadFileName =
                BuildUniqueStoredFileName(
                    file.Name,
                    "portfolio");

            _selectedPortfolioSizeLabel =
                FormatFileSize(file.Size);

            _selectedPortfolioContentType =
                NormalizePortfolioContentType(
                    file.Name,
                    file.ContentType);

            _selectedPortfolioBytes =
                bytes;

            SetOperation(
                "Portfolio document selected. Upload when ready.",
                Severity.Info);
        });
    }

    private async Task UploadPortfolioDirectAsync()
    {
        if (_selectedPortfolioBytes == null ||
            string.IsNullOrWhiteSpace(
                _selectedPortfolioUploadFileName) ||
            string.IsNullOrWhiteSpace(
                _selectedPortfolioFileName) ||
            string.IsNullOrWhiteSpace(
                _selectedPortfolioContentType))
        {
            SetOperation(
                "Please select a portfolio document first.",
                Severity.Warning);

            return;
        }

        await RunBusyAsync(async () =>
        {
            var displayFileName =
                _selectedPortfolioFileName!;

            var displayContentType =
                _selectedPortfolioContentType!;

            var updated =
                await ProfileApiClient.UpdatePortfolioAsync(
                    _currentUserId,
                    _selectedPortfolioBytes,
                    displayFileName,
                    displayContentType);

            var newPortfolioFileId =
                updated.PortfolioFileId
                ?? throw new InvalidOperationException(
                    "The updated portfolio FileResource id was not returned.");

            var portfolioFile =
                await ProfileApiClient.GetFileAsync(
                    newPortfolioFileId)
                ?? throw new InvalidOperationException(
                    "The uploaded portfolio file could not be loaded.");

            _currentUser = updated;

            _portfolioUrl =
                BuildVersionedUrl(
                    portfolioFile.CloudinarySecureUrl,
                    newPortfolioFileId);

            _portfolioFileName =
                portfolioFile.OriginalFileName;

            _portfolioTypeLabel =
                BuildDocumentTypeLabel(
                    portfolioFile.OriginalFileName,
                    portfolioFile.ContentType);

            ClearPortfolioSelection();

            SetOperation(
                "Portfolio uploaded successfully.",
                Severity.Success);

            Snackbar.Add(
                "Portfolio uploaded successfully.",
                Severity.Success);

            StateHasChanged();
        });
    }

    private async Task SendOtpForPasswordAsync()
    {
        if (string.IsNullOrWhiteSpace(
                _newPassword))
        {
            SetOperation(
                "New password cannot be empty.",
                Severity.Warning);

            return;
        }

        if (_newPassword.Length < 8)
        {
            SetOperation(
                "New password must be at least 8 characters.",
                Severity.Warning);

            return;
        }

        if (!string.Equals(
                _newPassword,
                _confirmPassword,
                StringComparison.Ordinal))
        {
            SetOperation(
                "Confirm password does not match.",
                Severity.Warning);

            return;
        }

        await RunBusyAsync(async () =>
        {
            await ProfilePasswordApiClient.SendOtpAsync(_currentUserId);

            _otpSentForPassword = true;
            _otpCodePassword = string.Empty;

            SetOperation(
                "OTP sent to your registered email.",
                Severity.Info);

            Snackbar.Add(
                "OTP sent to your registered email.",
                Severity.Info);
        });
    }

    private async Task ConfirmPasswordResetAsync()
    {
        if (string.IsNullOrWhiteSpace(
                _otpCodePassword))
        {
            SetOperation(
                "OTP code is required.",
                Severity.Warning);

            return;
        }

        await RunBusyAsync(async () =>
        {
            await ProfilePasswordApiClient.ResetPasswordAsync(
                _currentUserId,
                _otpCodePassword,
                _newPassword);

            _newPassword = string.Empty;
            _confirmPassword = string.Empty;
            _otpSentForPassword = false;
            _otpCodePassword = string.Empty;

            SetOperation(
                "Password reset successfully.",
                Severity.Success);

            Snackbar.Add(
                "Password reset successfully.",
                Severity.Success);
        });
    }

    private async Task RunBusyAsync(
        Func<Task> action)
    {
        if (_isBusy)
        {
            return;
        }

        _isBusy = true;

        try
        {
            await action();
        }
        catch (Exception ex)
        {
            SetOperation(
                ex.Message,
                Severity.Error);

            Snackbar.Add(
                ex.Message,
                Severity.Error);
        }
        finally
        {
            _isBusy = false;
        }
    }

    private void ClearAvatarSelection()
    {
        _avatarPreviewDataUrl = null;
        _selectedAvatarBytes = null;
        _selectedAvatarFileName = null;
        _selectedAvatarUploadFileName = null;
        _selectedAvatarSizeLabel = null;
        _selectedAvatarContentType = "image/jpeg";

        _avatarCropSourceDataUrl = null;
        _pendingAvatarOriginalFileName = null;
        _showAvatarCropDialog = false;
        _initializeAvatarCrop = false;
        _avatarCropReady = false;
        _avatarCropBusy = false;
        _avatarCropZoom = 1;

        _avatarInputKey++;
    }

    private void ClearPortfolioSelection()
    {
        _selectedPortfolioFileName = null;
        _selectedPortfolioUploadFileName = null;
        _selectedPortfolioSizeLabel = null;
        _selectedPortfolioBytes = null;
        _selectedPortfolioContentType = null;
    }

    private async Task<byte[]> ReadFileBytesAsync(
        IBrowserFile file)
    {
        await using var stream =
            file.OpenReadStream(
                MaxUploadBytes);

        using var memoryStream =
            new MemoryStream();

        await stream.CopyToAsync(
            memoryStream);

        return memoryStream.ToArray();
    }

    private static bool IsSupportedAvatarFile(
        string? fileName,
        string? contentType)
    {
        if (contentType?.ToLowerInvariant() is
            "image/png" or
            "image/jpeg" or
            "image/jpg" or
            "image/webp")
        {
            return true;
        }

        var extension =
            Path.GetExtension(
                fileName ?? string.Empty)
            .ToLowerInvariant();

        return extension is
            ".png" or
            ".jpg" or
            ".jpeg" or
            ".webp";
    }

    private static bool IsSupportedPortfolioFile(
        string? fileName,
        string? contentType)
    {
        if (contentType?.ToLowerInvariant() is
            "application/pdf" or
            "application/msword" or
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
        {
            return true;
        }

        var extension =
            Path.GetExtension(
                fileName ?? string.Empty)
            .ToLowerInvariant();

        return extension is
            ".pdf" or
            ".doc" or
            ".docx";
    }

    private static string NormalizeAvatarContentType(
        string? fileName,
        string? contentType)
    {
        var normalizedContentType =
            contentType?.ToLowerInvariant();

        if (normalizedContentType is
            "image/png" or
            "image/webp" or
            "image/jpeg")
        {
            return normalizedContentType;
        }

        if (normalizedContentType ==
            "image/jpg")
        {
            return "image/jpeg";
        }

        var extension =
            Path.GetExtension(
                fileName ?? string.Empty)
            .ToLowerInvariant();

        return extension switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "image/jpeg"
        };
    }

    private static string NormalizePortfolioContentType(
        string? fileName,
        string? contentType)
    {
        var normalizedContentType =
            contentType?.ToLowerInvariant();

        if (normalizedContentType is
            "application/pdf" or
            "application/msword" or
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
        {
            return normalizedContentType;
        }

        var extension =
            Path.GetExtension(
                fileName ?? string.Empty)
            .ToLowerInvariant();

        return extension switch
        {
            ".pdf" => "application/pdf",

            ".doc" =>
                "application/msword",

            ".docx" =>
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",

            _ => "application/pdf"
        };
    }

    private static async Task<byte[]> ResizeImageForPreviewAsync(
        byte[] sourceBytes,
        string contentType)
    {
        try
        {
            using var image = SixLabors.ImageSharp.Image.Load(sourceBytes);

            if (image.Width <= 512 && image.Height <= 512)
            {
                return sourceBytes;
            }

            image.Mutate(x => x.Resize(new SixLabors.ImageSharp.Processing.ResizeOptions
            {
                Mode = SixLabors.ImageSharp.Processing.ResizeMode.Max,
                Size = new SixLabors.ImageSharp.Size(512, 512)
            }));

            using var ms = new MemoryStream();
            image.Save(ms, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder());
            return ms.ToArray();
        }
        catch
        {
            return sourceBytes;
        }
    }

    private static string BuildDataUrl(
        string contentType,
        byte[] bytes)
    {
        return
            $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
    }

    private static string BuildInitial(
        string? displayName,
        string? username)
    {
        var source =
            !string.IsNullOrWhiteSpace(
                displayName)
                ? displayName.Trim()
                : username?.Trim();

        return string.IsNullOrWhiteSpace(
            source)
            ? "?"
            : source[..1].ToUpperInvariant();
    }

    private static string BuildVersionedUrl(
        string url,
        Guid fileResourceId)
    {
        var separator =
            url.Contains('?')
                ? "&"
                : "?";

        return
            $"{url}{separator}v={fileResourceId:N}";
    }

    private static string BuildUniqueStoredFileName(
        string originalFileName,
        string prefix)
    {
        var extension =
            Path.GetExtension(
                originalFileName);

        if (string.IsNullOrWhiteSpace(
                extension))
        {
            extension = ".bin";
        }

        return
            $"{prefix}_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
    }

    private static string BuildDocumentTypeLabel(
        string? fileName,
        string? contentType)
    {
        var extension =
            Path.GetExtension(
                fileName ?? string.Empty)
            .ToLowerInvariant();

        var normalizedContentType =
            contentType?.ToLowerInvariant();

        if (extension == ".pdf" ||
            normalizedContentType ==
            "application/pdf")
        {
            return "PDF";
        }

        if (extension == ".docx" ||
            normalizedContentType ==
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
        {
            return "DOCX";
        }

        if (extension == ".doc" ||
            normalizedContentType ==
            "application/msword")
        {
            return "DOC";
        }

        return "FILE";
    }

    private static string FormatFileSize(
        long bytes)
    {
        if (bytes >= 1024 * 1024)
        {
            return
                $"{bytes / 1024d / 1024d:0.##} MB";
        }

        if (bytes >= 1024)
        {
            return
                $"{bytes / 1024d:0.##} KB";
        }

        return $"{bytes} B";
    }

    private void SetOperation(
        string message,
        Severity severity)
    {
        _operationMessage = message;
        _operationSeverity = severity;
    }

    public async ValueTask DisposeAsync()
    {
        if (_avatarCropModule == null)
        {
            return;
        }

        try
        {
            await _avatarCropModule.InvokeVoidAsync(
                "dispose",
                AvatarCropCanvasId);

            await _avatarCropModule.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // Browser connection already closed.
        }
        catch (OperationCanceledException)
        {
            // Circuit disconnected during dispose.
        }
    }

    private enum ProfileSettingsSection
    {
        Profile,
        Portfolio,
        Security
    }
}
