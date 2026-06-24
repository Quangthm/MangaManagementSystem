using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Services
{
    public class SeriesProposalService : ISeriesProposalService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SeriesProposalService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<SeriesProposalDto?> GetProposalByIdAsync(Guid seriesProposalId, CancellationToken ct = default)
        {
            var entity = await _unitOfWork.SeriesProposals.GetByIdWithDetailsAsync(seriesProposalId, ct);
            if (entity == null) return null;

            var allContributors = await _unitOfWork.SeriesContributors.GetAllAsync();
            var hasTantou = allContributors.Any(c => c.SeriesId == entity.SeriesId && c.EndDate == null && c.User?.Role?.RoleName == "Tantou Editor");

            return MapToDto(entity, hasTantou);
        }

        public async Task<SeriesProposalDto?> GetLatestProposalBySeriesAsync(Guid seriesId, CancellationToken ct = default)
        {
            var entity = await _unitOfWork.SeriesProposals.GetLatestBySeriesIdAsync(seriesId, ct);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<IEnumerable<ProposalQueueItemDto>> GetEditorialQueueAsync(ProposalQueueFilterDto filter, CancellationToken ct = default)
        {
            var entities = await _unitOfWork.SeriesProposals.GetEditorialQueueAsync(
                filter.StatusCode, filter.SeriesId, filter.SubmittedByUserId, filter.ReviewedByUserId, ct);

            return entities.Select(p => new ProposalQueueItemDto(
                p.SeriesProposalId,
                p.SeriesId,
                p.Series?.Title ?? string.Empty,
                p.Series?.Slug ?? string.Empty,
                p.ProposalVersionNo,
                p.ProposalTitle,
                p.SynopsisSnapshot,
                MapGenres(p.Series?.Genres),
                MapTags(p.Series?.Tags),
                p.StatusCode,
                p.SubmittedByUserId,
                p.SubmittedByUser?.DisplayName ?? string.Empty,
                p.SubmittedAtUtc,
                p.ReviewedByUserId,
                p.ReviewedByUser?.DisplayName,
                p.ReviewedAtUtc,
                p.Comments,
                p.ProposalFileId,
                p.ProposalFile?.CloudinarySecureUrl,
                p.ProposalFile?.OriginalFileName,
                p.MarkupFileId,
                p.MarkupFile?.CloudinarySecureUrl
            ));
        }

        public async Task<SeriesProposalDto> CreateProposalAsync(CreateProposalDto dto, CancellationToken ct = default)
        {
            var entity = new SeriesProposal
            {
                SeriesId = dto.SeriesId,
                ProposalVersionNo = 1,
                ProposalTitle = dto.ProposalTitle,
                SynopsisSnapshot = dto.SynopsisSnapshot,
                GenreSnapshot = dto.GenreSnapshot,
                ProposalFileId = dto.ProposalFileId,
                StatusCode = "UNDER_EDITORIAL_REVIEW",
                SubmittedByUserId = dto.SubmittedByUserId,
                SubmittedAtUtc = DateTime.UtcNow
            };
            await _unitOfWork.SeriesProposals.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task ClaimEditorialReviewAsync(Guid seriesProposalId, Guid actorUserId, string? notes, CancellationToken ct = default)
        {
            await _unitOfWork.SeriesProposals.ClaimEditorialReviewAsync(seriesProposalId, actorUserId, notes, ct);
        }

        public async Task RequestRevisionAsync(Guid seriesProposalId, Guid actorUserId, string comments, FileUploadResultDto? markupFile, CancellationToken ct = default)
        {
            await _unitOfWork.SeriesProposals.RequestRevisionAsync(
                seriesProposalId, actorUserId, comments,
                markupFile?.OriginalFileName, markupFile?.PublicId, markupFile?.SecureUrl,
                markupFile?.ContentType, markupFile?.FileSizeBytes, markupFile?.Sha256Hash, ct);
        }

        public async Task PassToBoardAsync(Guid seriesProposalId, Guid actorUserId, string? comments, FileUploadResultDto? markupFile, CancellationToken ct = default)
        {
            await _unitOfWork.SeriesProposals.PassToBoardAsync(
                seriesProposalId, actorUserId, comments,
                markupFile?.OriginalFileName, markupFile?.PublicId, markupFile?.SecureUrl,
                markupFile?.ContentType, markupFile?.FileSizeBytes, markupFile?.Sha256Hash, ct);
        }

        public async Task CancelProposalAsync(Guid seriesProposalId, Guid actorUserId, string comments, FileUploadResultDto markupFile, CancellationToken ct = default)
        {
            if (markupFile == null) throw new ArgumentNullException(nameof(markupFile), "Markup file is required for cancellation.");
            
            await _unitOfWork.SeriesProposals.CancelProposalAsync(
                seriesProposalId, actorUserId, comments,
                markupFile.OriginalFileName, markupFile.PublicId, markupFile.SecureUrl,
                markupFile.ContentType, markupFile.FileSizeBytes, markupFile.Sha256Hash, ct);
        }

        private static SeriesProposalDto MapToDto(SeriesProposal p, bool hasTantou = false) => new(
            p.SeriesProposalId,
            p.SeriesId,
            p.ProposalVersionNo,
            p.ProposalTitle,
            p.SynopsisSnapshot,
            MapGenres(p.Series?.Genres),
            MapTags(p.Series?.Tags),
            p.ProposalFileId,
            p.StatusCode,
            p.SubmittedByUserId,
            p.SubmittedAtUtc,
            p.WithdrawnAtUtc,
            p.ReviewedByUserId,
            p.ReviewedAtUtc,
            p.Comments,
            p.MarkupFileId,
            hasTantou
        );

        private static IReadOnlyList<GenreDto> MapGenres(IEnumerable<Genre>? genres)
        {
            return genres?
                .OrderBy(g => g.GenreName)
                .Select(g => new GenreDto(g.GenreId, g.GenreName))
                .ToList()
                ?? new List<GenreDto>();
        }

        private static IReadOnlyList<TagDto> MapTags(IEnumerable<Tag>? tags)
        {
            return tags?
                .OrderBy(t => t.TagName)
                .Select(t => new TagDto(t.TagId, t.TagName))
                .ToList()
                ?? new List<TagDto>();
        }
    }
}
