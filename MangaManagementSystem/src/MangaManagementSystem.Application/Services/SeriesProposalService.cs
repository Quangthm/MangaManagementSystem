using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task<SeriesProposalDto> CreateSeriesProposalAsync(CreateSeriesProposalDto dto)
        {
            var entity = new SeriesProposal()
            {
                SeriesId = dto.SeriesId,
                ProposalVersionNo = dto.ProposalVersionNo,
                ProposalTitle = dto.ProposalTitle,
                SynopsisSnapshot = dto.SynopsisSnapshot,
                GenreSnapshot = dto.GenreSnapshot,
                ProposalFileId = dto.ProposalFileId,
                StatusCode = dto.StatusCode,
                SubmittedByUserId = dto.SubmittedByUserId,
                SubmittedAtUtc = DateTime.UtcNow,
                Comments = dto.Comments,
                MarkupFileId = dto.MarkupFileId,
            };
            await _unitOfWork.SeriesProposals.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<SeriesProposalDto?> GetSeriesProposalByIdAsync(Guid id)
        {
            var entity = await _unitOfWork.SeriesProposals.GetByIdAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<IEnumerable<SeriesProposalDto>> GetSeriesProposalsBySeriesIdAsync(Guid seriesId)
        {
            var all = await _unitOfWork.SeriesProposals.GetAllAsync();
            return all
                .Where(p => p.SeriesId == seriesId)
                .OrderBy(p => p.ProposalVersionNo)
                .Select(MapToDto);
        }

        public async Task<SeriesProposalDto?> UpdateSeriesProposalAsync(UpdateSeriesProposalDto dto)
        {
            var proposal = await _unitOfWork.SeriesProposals.GetByIdAsync(dto.SeriesProposalId);
            if (proposal == null)
            {
                return null;
            }

            proposal.SeriesId = dto.SeriesId;
            proposal.ProposalVersionNo = dto.ProposalVersionNo;
            proposal.ProposalTitle = dto.ProposalTitle;
            proposal.SynopsisSnapshot = dto.SynopsisSnapshot;
            proposal.GenreSnapshot = dto.GenreSnapshot;
            proposal.ProposalFileId = dto.ProposalFileId;
            proposal.StatusCode = dto.StatusCode;
            proposal.SubmittedByUserId = dto.SubmittedByUserId;
            proposal.ReviewedByUserId = dto.ReviewedByUserId;
            proposal.ReviewedAtUtc = dto.ReviewedAtUtc;
            proposal.Comments = dto.Comments;
            proposal.MarkupFileId = dto.MarkupFileId;
            _unitOfWork.SeriesProposals.Update(proposal);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(proposal);
        }

        public async Task<bool> DeleteSeriesProposalAsync(Guid id)
        {
            var entity = await _unitOfWork.SeriesProposals.GetByIdAsync(id);
            if (entity == null)
            {
                return false;
            }

            _unitOfWork.SeriesProposals.Delete(entity);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private static SeriesProposalDto MapToDto(SeriesProposal p) => new(
            p.SeriesProposalId,
            p.SeriesId,
            p.ProposalVersionNo,
            p.ProposalTitle,
            p.SynopsisSnapshot,
            p.GenreSnapshot,
            p.ProposalFileId,
            p.StatusCode,
            p.SubmittedByUserId,
            p.SubmittedAtUtc,
            p.WithdrawnAtUtc,
            p.ReviewedByUserId,
            p.ReviewedAtUtc,
            p.Comments,
            p.MarkupFileId
        );
    }
}
