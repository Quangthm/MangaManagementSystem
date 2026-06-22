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

namespace MangaManagementSystem.Application.Features.Mangaka.Series.Commands.CreateSeriesDraft
{
    /// <summary>
    /// Handles Create Series Draft (BF-SERIES-001).
    /// Logic lifted from SeriesService.CreateSeriesDraftAsync and extended with:
    ///   - cover file type/size validation before Cloudinary upload,
    ///   - SHA-256 null guard after upload.
    ///
    /// Orchestration:
    ///   1. Validate actor, title, genre.
    ///   2. Normalize slug, language code, publication frequency code.
    ///   3. (Optional) Validate cover format and size.
    ///   4. (Optional) Upload cover to Cloudinary via IFileStorageService.
    ///   5. Guard SHA-256 hash returned from upload.
    ///   6. Call manga.usp_Series_Create through ISeriesRepository.
    ///   7. If SQL fails after upload: best-effort Cloudinary cleanup.
    ///   8. Return SeriesDraftCreatedDto.
    ///
    /// Creates ONLY: manga.Series (PROPOSAL_DRAFT) + active SeriesContributor +
    ///               optional SERIES_COVER FileResource.
    /// Must NOT create: SeriesProposal, SERIES_PROPOSAL file, editorial records.
    /// </summary>
    public sealed class CreateSeriesDraftCommandHandler
        : IRequestHandler<CreateSeriesDraftCommand, SeriesDraftCreatedDto>
    {
        private const string DefaultLanguageCode    = "ja";
        private const string SeriesCoverPurpose     = "SERIES_COVER";
        private const string ProposalDraftStatus    = "PROPOSAL_DRAFT";
        private const string CloudinaryImageType    = "image";
        private const long   MaxCoverFileSizeBytes  = 5 * 1024 * 1024; // 5 MB

        private static readonly HashSet<string> AllowedCoverExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

        private static readonly HashSet<string> AllowedCoverContentTypes =
            new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };

        private readonly ISeriesRepository       _seriesRepository;
        private readonly IFileStorageService     _fileStorageService;
        private readonly ILogger<CreateSeriesDraftCommandHandler> _logger;

        public CreateSeriesDraftCommandHandler(
            ISeriesRepository seriesRepository,
            IFileStorageService fileStorageService,
            ILogger<CreateSeriesDraftCommandHandler> logger)
        {
            _seriesRepository   = seriesRepository;
            _fileStorageService = fileStorageService;
            _logger             = logger;
        }

        public async Task<SeriesDraftCreatedDto> Handle(
            CreateSeriesDraftCommand command,
            CancellationToken cancellationToken)
        {
            // ── 1. Input validation ──────────────────────────────────────────────────
            if (command.ActorUserId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "A valid signed-in user is required to create a series draft.");
            }

            string title = command.Title?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new InvalidOperationException(
                    "A title is required to create a series draft.");
            }

            IReadOnlyList<Guid> genreIds = CleanGuidList(command.GenreIds);
            if (genreIds.Count == 0)
            {
                throw new InvalidOperationException(
                    "At least one genre is required to create a series draft.");
            }

            IReadOnlyList<Guid> tagIds = CleanGuidList(command.TagIds ?? Array.Empty<Guid>());

            string synopsis = string.IsNullOrWhiteSpace(command.Synopsis)
                ? title
                : command.Synopsis.Trim();

            string contentLanguageCode = string.IsNullOrWhiteSpace(command.ContentLanguageCode)
                ? DefaultLanguageCode
                : command.ContentLanguageCode.Trim().ToLowerInvariant();

            string slug = SlugGenerator.Normalize(command.Slug, title);
            if (string.IsNullOrWhiteSpace(slug))
            {
                throw new InvalidOperationException(
                    "Could not derive a URL slug from the title. Please adjust the title.");
            }

            string? publicationFrequencyCode = string.IsNullOrWhiteSpace(command.PublicationFrequencyCode)
                ? null
                : command.PublicationFrequencyCode.Trim().ToUpperInvariant();

            // ── 2. Optional cover validation (before upload) ─────────────────────────
            bool hasCover = command.CoverFileBytes is { Length: > 0 };

            if (hasCover)
            {
                if (string.IsNullOrWhiteSpace(command.CoverFileName))
                {
                    throw new InvalidOperationException(
                        "The cover file name is missing. Please re-select the image and try again.");
                }

                if (string.IsNullOrWhiteSpace(command.CoverContentType))
                {
                    throw new InvalidOperationException(
                        "The cover file type could not be determined. Please re-select the image.");
                }

                if (command.CoverFileBytes!.Length > MaxCoverFileSizeBytes)
                {
                    throw new InvalidOperationException(
                        "The cover image exceeds the maximum allowed size of 5 MB.");
                }

                var ext = Path.GetExtension(command.CoverFileName);
                if (!AllowedCoverExtensions.Contains(ext))
                {
                    throw new InvalidOperationException(
                        "Only JPG, PNG, and WEBP images are accepted as series covers.");
                }

                if (!AllowedCoverContentTypes.Contains(command.CoverContentType))
                {
                    throw new InvalidOperationException(
                        "The cover file type is not accepted. Please upload a JPG, PNG, or WEBP image.");
                }
            }

            // ── 3. Optional cover upload to Cloudinary ───────────────────────────────
            string? coverOriginalFileName  = null;
            string? coverPublicId          = null;
            string? coverSecureUrl         = null;
            string? coverContentType       = null;
            long?   coverFileSizeBytes     = null;
            string? coverSha256Hash        = null;

            if (hasCover)
            {
                var uploadResult = await _fileStorageService.UploadFileAsync(
                    command.CoverFileBytes!,
                    command.CoverFileName!,
                    command.CoverContentType!,
                    SeriesCoverPurpose,
                    uploadedByUserId: null);

                // ── 4. SHA-256 null guard ────────────────────────────────────────────
                if (string.IsNullOrWhiteSpace(uploadResult.Sha256Hash))
                {
                    await TryCleanupCoverAsync(uploadResult.PublicId,
                        "SHA-256 hash was not returned by the file storage service after cover upload.");
                    throw new InvalidOperationException(
                        "The cover image integrity check could not be completed. Please try again.");
                }

                coverOriginalFileName = uploadResult.OriginalFileName;
                coverPublicId         = uploadResult.PublicId;
                coverSecureUrl        = uploadResult.SecureUrl;
                coverContentType      = uploadResult.ContentType;
                coverFileSizeBytes    = uploadResult.FileSizeBytes;
                coverSha256Hash       = uploadResult.Sha256Hash;
            }

            // ── 5. Call stored procedure through repository ──────────────────────────
            Guid  newSeriesId;
            Guid? coverFileResourceId;

            try
            {
                (newSeriesId, coverFileResourceId) =
                    await _seriesRepository.CreateSeriesDraftViaProcAsync(
                        actorUserId:               command.ActorUserId,
                        title:                     title,
                        slug:                      slug,
                        synopsis:                  synopsis,
                        genreIds:                  genreIds,
                        tagIds:                    tagIds,
                        contentLanguageCode:        contentLanguageCode,
                        sourceSeriesId:             command.SourceSeriesId,
                        publicationFrequencyCode:   publicationFrequencyCode,
                        coverOriginalFileName:      coverOriginalFileName,
                        coverCloudinaryPublicId:    coverPublicId,
                        coverCloudinarySecureUrl:   coverSecureUrl,
                        coverContentType:           coverContentType,
                        coverFileSizeBytes:         coverFileSizeBytes,
                        coverSha256Hash:            coverSha256Hash,
                        cancellationToken:          cancellationToken);
            }
            catch (Exception ex)
            {
                // Cloudinary upload already succeeded — attempt best-effort cleanup so the
                // uploaded file does not become an orphan in Cloudinary storage.
                if (hasCover && !string.IsNullOrEmpty(coverPublicId))
                {
                    await TryCleanupCoverAsync(coverPublicId,
                        $"SQL workflow failed after cover upload for new series draft by actor {command.ActorUserId}.");
                }

                _logger.LogError(ex,
                    "Failed to create series draft for actor {ActorUserId}.", command.ActorUserId);
                throw;
            }

            return new SeriesDraftCreatedDto(
                newSeriesId,
                title,
                slug,
                ProposalDraftStatus,
                coverFileResourceId);
        }

        /// <summary>
        /// Best-effort Cloudinary cleanup for cover images after a SQL workflow failure.
        /// Cover images use "image" Cloudinary resource type.
        /// Cleanup failure is logged safely; the original error is still rethrown by the caller.
        /// </summary>
        private async Task TryCleanupCoverAsync(string publicId, string reason)
        {
            try
            {
                await _fileStorageService.DeleteFileAsync(publicId, CloudinaryImageType);
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
