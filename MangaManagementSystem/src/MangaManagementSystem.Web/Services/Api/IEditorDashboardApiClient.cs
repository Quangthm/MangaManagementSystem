using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Editor;

namespace MangaManagementSystem.Web.Services.Api
{
    /// <summary>
    /// Typed client for the Tantou Editor dashboard read model. Keeps the Razor layer free of
    /// direct Application/Infrastructure/EF access — it only calls the API over HTTP.
    /// </summary>
    public interface IEditorDashboardApiClient
    {
        /// <summary>
        /// Loads the editor dashboard (KPI counts, proposal queue preview, recent series
        /// activity).
        /// </summary>
        Task<EditorDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
    }
}
