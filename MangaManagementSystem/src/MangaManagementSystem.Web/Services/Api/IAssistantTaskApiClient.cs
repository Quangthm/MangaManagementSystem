using MangaManagementSystem.Application.DTOs.Manga;
using Microsoft.AspNetCore.Components.Forms;

namespace MangaManagementSystem.Web.Services.Api
{
    public interface IAssistantTaskApiClient
    {
        // Read operations
        Task<IReadOnlyList<ChapterPageTaskDto>> GetAssignedTasksAsync(CancellationToken cancellationToken = default);
        Task<ChapterPageTaskDto?> GetTaskDetailAsync(Guid taskId, CancellationToken cancellationToken = default);

        // Submit operation
        Task<AssistantTaskSubmitResultDto?> SubmitTaskWorkAsync(
            Guid taskId,
            IBrowserFile file,
            string? versionNote = null,
            CancellationToken cancellationToken = default);
    }
}
