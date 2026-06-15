using MangaManagementSystem.Application.DTOs.Manga;
using Microsoft.AspNetCore.Components.Forms;

namespace MangaManagementSystem.Web.Services.Api
{
    public interface IAssistantTaskApiClient
    {
        Task<AssistantTaskSubmitResultDto?> SubmitTaskWorkAsync(
            Guid taskId,
            IBrowserFile file,
            string? versionNote = null,
            CancellationToken cancellationToken = default);
    }
}
