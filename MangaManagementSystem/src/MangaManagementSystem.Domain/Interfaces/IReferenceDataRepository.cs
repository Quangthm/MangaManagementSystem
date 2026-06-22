using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Domain.Entities;

namespace MangaManagementSystem.Domain.Interfaces
{
    public interface IReferenceDataRepository
    {
        Task<IReadOnlyList<Genre>> GetGenresAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Tag>> GetTagsAsync(CancellationToken cancellationToken = default);
    }
}
