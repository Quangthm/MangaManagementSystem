using MangaManagementSystem.Application.DTOs.Manga;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MangaManagementSystem.Application.Interfaces
{
    public interface IChapterReaderVoteSnapshotService
    {
        Task<ChapterReaderVoteSnapshotDto> CreateChapterReaderVoteSnapshotAsync(CreateChapterReaderVoteSnapshotDto dto);
        Task<ChapterReaderVoteSnapshotDto?> GetChapterReaderVoteSnapshotByIdAsync(long id);
        Task<IEnumerable<ChapterReaderVoteSnapshotDto>> GetChapterReaderVoteSnapshotsByChapterIdAsync(long chapterId);
    }
}
