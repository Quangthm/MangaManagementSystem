using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MangaManagementSystem.Application.Common;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.Application.Features.Mangaka.Series.Commands.UpdateSeriesDraft
{
    /// <summary>
    /// Handles BF-SERIES-002 — Edit Series Draft Profile.
    ///
    /// Orchestration:
    ///   1. Validate command inputs.
    ///   2. Normalize slug from title or explicit slug.
    ///   3. (Optional) Upload new cover to Cloudinary via IFileStorageService.
    ///   4. Call manga.usp_Series_UpdateProfile through ISeriesRepository.
    ///   5. If SQL fails after Cloudinary upload, attempt best-effort Cloudinary cleanup.
    ///   6. Return SeriesDraftUpdatedDto.
    ///
    /// Cover upload is optional. When no cover file bytes are provided, the stored
    /// procedure keeps the existing cover unchanged (cover_file_id is not touched).
    ///
    /// Only PROPOSAL_DRAFT series are accepted. The stored procedure enforces the
    /// status guard, contributor permission, slug uniqueness, and audit event.
    /// Cloudinary cover images use "image" resource type for cleanup.
    /// </summary>
    public sealed class UpdateSeriesDraftCommandHandler
        : IRequestHandler<UpdateSeriesDraftCommand, SeriesDraftUpdatedDto>
    {
        private const string SeriesCoverPurpose = "SERIES_COVER";
        private const string CloudinaryImageResourceType = "image";

        private readonly ISeriesRepository _seriesRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<UpdateSeriesDraftCommandHandler> _logger;

        public UpdateSeriesDraftCommandHandler(
            ISeriesRepository seriesRepository,
            IFileStorageService fileStorageService,
            ILogger<UpdateSeriesDraftCommandHandler> logger)
        {
            _seriesRepository = seriesRepository;
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        public async Task<SeriesDraftUpdatedDto> Handle(
            UpdateSeriesDraftCommand command,
            CancellationToken cancellationToken)
        {
            // ── 1. Input validation ──────────────────────────────────────────────────
            if (command.ActorUserId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "A valid signed-in user is required to update a series draft.");
            }

            if (command.SeriesId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "A valid series must be selected to update a draft.");
            }

            string title = command.Title?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new InvalidOperationException("A title is required.");
            }

            IReadOnlyList<Guid> genreIds = CleanGuidList(command.GenreIds);
            if (genreIds.Count == 0)
            {
                throw new InvalidOperationException("At least one genre is required.");
            }

            IReadOnlyList<Guid> tagIds = CleanGuidList(command.TagIds ?? Array.Empty<Guid>());

            if (string.IsNullOrWhiteSpace(command.Synopsis))
            {
                throw new InvalidOperationException("Synopsis / Description is required.");
            }

            string synopsis = command.Synopsis.Trim();

            string contentLanguageCode = string.IsNullOrWhiteSpace(command.ContentLanguageCode)
                ? "ja"
                : command.ContentLanguageCode.Trim().ToLowerInvariant();

            string? publicationFrequencyCode = string.IsNullOrWhiteSpace(command.PublicationFrequencyCode)
                ? null
                : command.PublicationFrequencyCode.Trim().ToUpperInvariant();

            // ── 2. Slug normalization ────────────────────────────────────────────────
            string slug = SlugGenerator.Normalize(command.Slug, title);
            if (string.IsNullOrWhiteSpace(slug))
            {
                throw new InvalidOperationException(
                    "Could not derive a URL slug from the title. Please adjust the title.");
            }

            // ── 3. Optional cover upload ─────────────────────────────────────────────
            bool hasCover = command.CoverFileBytes is { Length: > 0 };

            string? coverOriginalFileName = null;
            string? coverPublicId = null;
            string? coverSecureUrl = null;
            string? coverContentType = null;
            long? coverFileSizeBytes = null;
            string? coverSha256Hash = null;

            if (hasCover)
            {
                var uploadResult = await _fileStorageService.UploadFileAsync(
                    command.CoverFileBytes!,
                    command.CoverFileName ?? "cover.png",
                    command.CoverContentType ?? "image/png",
                    SeriesCoverPurpose,
                    uploadedByUserId: null);

                if (string.IsNullOrWhiteSpace(uploadResult.Sha256Hash))
                {
                    await TryCleanupCoverAsync(uploadResult.PublicId,
                        "SHA-256 hash was not returned by the file storage service.");
                    throw new InvalidOperationException(
                        "The cover image integrity check could not be completed. Please try again.");
                }

                coverOriginalFileName = uploadResult.OriginalFileName;
                coverPublicId = uploadResult.PublicId;
                coverSecureUrl = uploadResult.SecureUrl;
                coverContentType = uploadResult.ContentType;
                coverFileSizeBytes = uploadResult.FileSizeBytes;
                coverSha256Hash = uploadResult.Sha256Hash;
            }

            // ── 4. Call stored procedure through repository ──────────────────────────
            Guid? newCoverFileResourceId;

            try
            {
                newCoverFileResourceId = await _seriesRepository.UpdateSeriesDraftViaProcAsync(
                    actorUserId: command.ActorUserId,
                    seriesId: command.SeriesId,
                    title: title,
                    slug: slug,
                    synopsis: synopsis,
                    genreIds: genreIds,
                    tagIds: tagIds,
                    contentLanguageCode: contentLanguageCode,
                    publicationFrequencyCode: publicationFrequencyCode,
                    coverOriginalFileName: coverOriginalFileName,
                    coverCloudinaryPublicId: coverPublicId,
                    coverCloudinarySecureUrl: coverSecureUrl,
                    coverContentType: coverContentType,
                    coverFileSizeBytes: coverFileSizeBytes,
                    coverSha256Hash: coverSha256Hash,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                if (hasCover && !string.IsNullOrEmpty(coverPublicId))
                {
                    await TryCleanupCoverAsync(coverPublicId,
                        $"SQL workflow failed after Cloudinary cover upload for series {command.SeriesId}.");
                }

                _logger.LogError(ex,
                    "Failed to update series draft profile for series {SeriesId} by actor {ActorUserId}.",
                    command.SeriesId, command.ActorUserId);

                throw;
            }

            return new SeriesDraftUpdatedDto
            {
                SeriesId = command.SeriesId,
                Title = title,
                Slug = slug,
                Synopsis = synopsis,
                ContentLanguageCode = contentLanguageCode,
                PublicationFrequencyCode = publicationFrequencyCode,
                NewCoverFileResourceId = newCoverFileResourceId,
                // Surface the cover URL so the Web can update the in-memory card without reload.
                NewCoverUrl = hasCover ? coverSecureUrl : null
            };
        }

        /// <summary>
        /// Best-effort Cloudinary cleanup for cover images after a SQL workflow failure.
        /// Cover images use "image" resource type in Cloudinary.
        /// Cleanup failure is logged safely; the original error is still rethrown by the caller.
        /// </summary>
        private async Task TryCleanupCoverAsync(string publicId, string reason)
        {
            try
            {
                await _fileStorageService.DeleteFileAsync(publicId, CloudinaryImageResourceType);
            }
            catch (Exception cleanupEx)
            {
                _logger.LogError(
                    cleanupEx,
                    "Failed to clean up cover image {PublicId} from Cloudinary after failure. Reason: {Reason}",
                    publicId, reason);
            }
        }

        private static IReadOnlyList<Guid> CleanGuidList(IEnumerable<Guid> ids)
        {
            return ids
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToArray();
        }
    }
}
