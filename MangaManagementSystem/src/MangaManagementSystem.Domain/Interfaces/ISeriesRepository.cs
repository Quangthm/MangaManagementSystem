using MangaManagementSystem.Domain.Entities;
using System.Threading.Tasks;

namespace MangaManagementSystem.Domain.Interfaces
{
    public interface ISeriesRepository : IGenericRepository<Series>
    {
        Task<Series?> GetSeriesWithChaptersAsync(long seriesId);
    }
}
