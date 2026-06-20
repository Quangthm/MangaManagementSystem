using MangaManagementSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Domain.Interfaces
{
    public interface IEditorSeriesRepository
    {
        Task<IReadOnlyList<Series>> GetSeriesAsync(Guid actorUserId, CancellationToken ct = default);
    }
}
