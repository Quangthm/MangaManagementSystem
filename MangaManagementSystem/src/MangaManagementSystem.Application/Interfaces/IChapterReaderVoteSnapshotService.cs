using MangaManagementSystem.Application.DTOs.Manga;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface IChapterReaderVoteSnapshotService
    {
        Task<ChapterReaderVoteSnapshotDto> CreateChapterReaderVoteSnapshotAsync(CreateChapterReaderVoteSnapshotDto dto);
        Task<ChapterReaderVoteSnapshotDto?> GetChapterReaderVoteSnapshotByIdAsync(Guid id);
        Task<IEnumerable<ChapterReaderVoteSnapshotDto>> GetChapterReaderVoteSnapshotsByChapterIdAsync(Guid chapterId);
    }
}
