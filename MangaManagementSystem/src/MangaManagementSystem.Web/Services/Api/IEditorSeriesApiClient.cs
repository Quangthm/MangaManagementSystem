using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Editor;

namespace MangaManagementSystem.Web.Services.Api
{
    public interface IEditorSeriesApiClient
    {
        Task<EditorSeriesListDto> GetSeriesAsync(CancellationToken cancellationToken = default);
    }
}
