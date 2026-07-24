using System;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Editor;

namespace MangaManagementSystem.Web.Services.Api
{
    public interface IEditorAnnotationApiClient
    {
        Task<EditorAnnotationWorkspaceDto> GetAnnotationsAsync(
            string? seriesId = null,
            string? issueType = null,
            string? status = null,
            CancellationToken cancellationToken = default);
    }
}
