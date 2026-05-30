using MangaManagementSystem.Application.DTOs.Manga;
using MangaManagementSystem.Application.Interfaces;
using MangaManagementSystem.Domain.Entities;
using MangaManagementSystem.Domain.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Services
{
    public class ChapterReaderVoteSnapshotService : IChapterReaderVoteSnapshotService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ChapterReaderVoteSnapshotService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ChapterReaderVoteSnapshotDto> CreateChapterReaderVoteSnapshotAsync(CreateChapterReaderVoteSnapshotDto dto)
        {
            var entity = new ChapterReaderVoteSnapshot
            {
                ChapterId = dto.ChapterId,
                ReaderVoteCount = dto.ReaderVoteCount,
                AverageRating = dto.AverageRating,
                EnteredByUserId = dto.EnteredByUserId
            };
            await _unitOfWork.ChapterReaderVoteSnapshots.AddAsync(entity);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(entity);
        }

        public async Task<ChapterReaderVoteSnapshotDto?> GetChapterReaderVoteSnapshotByIdAsync(long id)
        {
            var entity = await _unitOfWork.ChapterReaderVoteSnapshots.GetByIdAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<IEnumerable<ChapterReaderVoteSnapshotDto>> GetChapterReaderVoteSnapshotsByChapterIdAsync(long chapterId)
        {
            var all = await _unitOfWork.ChapterReaderVoteSnapshots.GetAllAsync();
            return all.Where(s => s.ChapterId == chapterId).Select(MapToDto);
        }

        private static ChapterReaderVoteSnapshotDto MapToDto(ChapterReaderVoteSnapshot s) => new(
            s.ChapterReaderVoteSnapshotId,
            s.ChapterId,
            s.ReaderVoteCount,
            s.AverageRating,
            s.EnteredByUserId
        );
    }
}
