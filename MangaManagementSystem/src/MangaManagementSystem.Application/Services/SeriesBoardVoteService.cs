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
    public class SeriesBoardVoteService : ISeriesBoardVoteService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SeriesBoardVoteService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<SeriesBoardVoteDto> CreateSeriesBoardVoteAsync(CreateSeriesBoardVoteDto dto)
        {
            var entity = new SeriesBoardVote
            {
                SeriesBoardPollId = dto.SeriesBoardPollId,
                UserId = dto.UserId,
                ChoiceCode = dto.ChoiceCode,
                VoteReason = dto.VoteReason,
                VotedAtUtc = DateTime.UtcNow
            };
            await _unitOfWork.SeriesBoardVotes.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<SeriesBoardVoteDto?> GetSeriesBoardVoteByIdAsync(Guid id)
        {
            var entity = await _unitOfWork.SeriesBoardVotes.GetByIdAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<IEnumerable<SeriesBoardVoteDto>> GetSeriesBoardVotesByPollIdAsync(Guid seriesBoardPollId)
        {
            var all = await _unitOfWork.SeriesBoardVotes.GetAllAsync();
            return all.Where(v => v.SeriesBoardPollId == seriesBoardPollId).Select(MapToDto);
        }

        private static SeriesBoardVoteDto MapToDto(SeriesBoardVote v) => new(
            v.SeriesBoardVoteId,
            v.SeriesBoardPollId,
            v.UserId,
            v.ChoiceCode,
            v.VoteReason,
            v.VotedAtUtc
        );
    }
}
