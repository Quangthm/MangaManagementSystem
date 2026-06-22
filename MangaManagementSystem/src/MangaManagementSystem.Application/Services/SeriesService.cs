using MangaManagementSystem.Application.Common;
using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Services
{
    public class SeriesService : ISeriesService
    {
        private const string DefaultContentLanguageCode = "ja";
        private const string SeriesCoverPurpose = "SERIES_COVER";
        private const string ProposalDraftStatus = "PROPOSAL_DRAFT";

        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<SeriesService> _logger;

        public SeriesService(
            IUnitOfWork unitOfWork,
            IFileStorageService fileStorageService,
            ILogger<SeriesService> logger)
        {
            _unitOfWork = unitOfWork;
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        public async Task<SeriesDto> CreateSeriesAsync(CreateSeriesDto dto)
        {
            var entity = new Series
            {
                Title = dto.Title,
                Slug = dto.Slug,
                Synopsis = dto.Synopsis,
                CoverFileId = dto.CoverFileId,
                StatusCode = dto.StatusCode,
                ContentLanguageCode = dto.ContentLanguageCode,
                SourceSeriesId = dto.SourceSeriesId,
                PublicationFrequencyCode = dto.PublicationFrequencyCode,
                CreatedAtUtc = System.DateTime.UtcNow
            };
            await _unitOfWork.Series.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<SeriesDraftCreatedDto> CreateSeriesDraftAsync(
            Guid actorUserId,
            CreateSeriesDraftDto dto,
            CancellationToken cancellationToken = default)
        {
            if (dto is null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            if (actorUserId == Guid.Empty)
            {
                throw new InvalidOperationException("A valid signed-in user is required to create a series draft.");
            }

            string title = dto.Title?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new InvalidOperationException("A title is required to create a series draft.");
            }

            string synopsis = string.IsNullOrWhiteSpace(dto.Synopsis) ? title : dto.Synopsis.Trim();
            string genre = dto.Genre?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(genre))
            {
                throw new InvalidOperationException("A genre is required to create a series draft.");
            }

            string contentLanguageCode = string.IsNullOrWhiteSpace(dto.ContentLanguageCode)
                ? DefaultContentLanguageCode
                : dto.ContentLanguageCode.Trim().ToLowerInvariant();

            string slug = SlugGenerator.Normalize(dto.Slug, title);
            if (string.IsNullOrWhiteSpace(slug))
            {
                throw new InvalidOperationException("Could not derive a URL slug from the title. Please adjust the title.");
            }

            string? publicationFrequencyCode = string.IsNullOrWhiteSpace(dto.PublicationFrequencyCode)
                ? null
                : dto.PublicationFrequencyCode.Trim().ToUpperInvariant();

            // Optional cover: upload to Cloudinary first (outside the SQL transaction), then pass
            // the resulting metadata to the stored procedure, which creates the FileResource itself.
            string? coverOriginalFileName = null;
            string? coverPublicId = null;
            string? coverSecureUrl = null;
            string? coverContentType = null;
            long? coverFileSizeBytes = null;
            string? coverSha256Hash = null;

            bool hasCover = dto.CoverFileBytes is { Length: > 0 };

            if (hasCover)
            {
                var uploadResult = await _fileStorageService.UploadFileAsync(
                    dto.CoverFileBytes!,
                    dto.CoverFileName ?? "cover.png",
                    dto.CoverContentType ?? "image/png",
                    SeriesCoverPurpose,
                    null);

                coverOriginalFileName = uploadResult.OriginalFileName;
                coverPublicId = uploadResult.PublicId;
                coverSecureUrl = uploadResult.SecureUrl;
                coverContentType = uploadResult.ContentType;
                coverFileSizeBytes = uploadResult.FileSizeBytes;
                coverSha256Hash = uploadResult.Sha256Hash;
            }

            Guid newSeriesId;
            Guid? coverFileResourceId;

            try
            {
                (newSeriesId, coverFileResourceId) = await _unitOfWork.Series.CreateSeriesDraftViaProcAsync(
                    actorUserId,
                    title,
                    slug,
                    synopsis,
                    new List<Guid>(), // Transitional: genre IDs not yet supported through legacy service
                    new List<Guid>(), // Transitional: tag IDs not yet supported through legacy service
                    contentLanguageCode,
                    dto.SourceSeriesId,
                    publicationFrequencyCode,
                    coverOriginalFileName,
                    coverPublicId,
                    coverSecureUrl,
                    coverContentType,
                    coverFileSizeBytes,
                    coverSha256Hash,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                // Cloudinary upload already succeeded, so attempt cleanup if the SQL workflow fails.
                if (hasCover && !string.IsNullOrEmpty(coverPublicId))
                {
                    await TryDeleteUploadedCoverAsync(coverPublicId!);
                }

                _logger.LogError(ex, "Failed to create series draft for actor {ActorUserId}.", actorUserId);
                throw;
            }

            return new SeriesDraftCreatedDto(
                newSeriesId,
                title,
                slug,
                ProposalDraftStatus,
                coverFileResourceId);
        }

        public async Task<SeriesDto?> GetSeriesByIdAsync(Guid id)
        {
            var entity = await _unitOfWork.Series.GetByIdAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<IEnumerable<SeriesDto>> GetAllSeriesAsync()
        {
            // Use the cover-included query so the dashboard can display cover thumbnails
            // without a per-card N+1 fetch. Read-only; no tracking required.
            var entities = await _unitOfWork.Series.GetAllWithCoverAsync();
            return entities.Select(MapToDto);
        }

        public async Task<SeriesDto?> UpdateSeriesAsync(UpdateSeriesDto dto)
        {
            var entity = await _unitOfWork.Series.GetByIdAsync(dto.SeriesId);
            if (entity == null) return null;

            entity.Title = dto.Title;
            entity.Slug = dto.Slug;
            entity.Synopsis = dto.Synopsis;
            entity.CoverFileId = dto.CoverFileId;
            entity.StatusCode = dto.StatusCode;
            entity.ContentLanguageCode = dto.ContentLanguageCode;
            entity.SourceSeriesId = dto.SourceSeriesId;
            entity.PublicationFrequencyCode = dto.PublicationFrequencyCode;
            entity.UpdatedAtUtc = DateTime.UtcNow;
            entity.UpdatedByUserId = dto.UpdatedByUserId;

            _unitOfWork.Series.Update(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        private async Task TryDeleteUploadedCoverAsync(string coverPublicId)
        {
            try
            {
                // Covers are images in Cloudinary.
                await _fileStorageService.DeleteFileAsync(coverPublicId, "image");
            }
            catch (Exception cleanupEx)
            {
                _logger.LogError(
                    cleanupEx,
                    "Failed to clean up uploaded cover {PublicId} after series draft creation failure.",
                    coverPublicId);
            }
        }

        private static SeriesDto MapToDto(Series s) => new(
            s.SeriesId,
            s.Title,
            s.Slug,
            s.Synopsis,
            MapGenres(s.Genres),
            MapTags(s.Tags),
            s.CoverFileId,
            s.StatusCode,
            s.ContentLanguageCode,
            s.SourceSeriesId,
            s.CreatedAtUtc,
            s.UpdatedAtUtc,
            s.UpdatedByUserId,
            s.PublicationFrequencyCode,
            // CoverUrl: populated only when the CoverFile navigation is loaded and not deleted.
            // Display-only — never used for upload/update workflows.
            CoverUrl: s.CoverFile?.DeletedAtUtc == null
                ? s.CoverFile?.CloudinarySecureUrl
                : null
        );

        private static IReadOnlyList<GenreDto> MapGenres(IEnumerable<Genre> genres)
        {
            return genres
                .OrderBy(g => g.GenreName)
                .Select(g => new GenreDto(g.GenreId, g.GenreName))
                .ToList();
        }

        private static IReadOnlyList<TagDto> MapTags(IEnumerable<Tag> tags)
        {
            return tags
                .OrderBy(t => t.TagName)
                .Select(t => new TagDto(t.TagId, t.TagName))
                .ToList();
        }
    }
}
