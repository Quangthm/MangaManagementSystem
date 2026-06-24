using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;

namespace MangaManagementSystem.Web.Services.Api
{
    public interface IReferenceDataApiClient
    {
        Task<IReadOnlyList<GenreDto>> GetGenresAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TagDto>> GetTagsAsync(CancellationToken cancellationToken = default);
    }
}
