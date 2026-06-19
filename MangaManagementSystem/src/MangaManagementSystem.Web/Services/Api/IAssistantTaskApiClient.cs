using MangaManagementSystem.Application.DTOs.Manga;
using Microsoft.AspNetCore.Components.Forms;

namespace MangaManagementSystem.Web.Services.Api
{
    public interface IAssistantTaskApiClient
    {
        // Read operations
        Task<IReadOnlyList<ChapterPageTaskDto>> GetAssignedTasksAsync(Guid actorUserId, CancellationToken cancellationToken = default);
        Task<ChapterPageTaskDto?> GetTaskDetailAsync(Guid actorUserId, Guid taskId, CancellationToken cancellationToken = default);

        // Annotations for assigned regions
        Task<IReadOnlyList<ChapterPageAnnotationDto>> GetTaskAnnotationsAsync(Guid actorUserId, Guid taskId, CancellationToken cancellationToken = default);

        // Submit operation
        Task<AssistantTaskSubmitResultDto?> SubmitTaskWorkAsync(
            Guid actorUserId,
            Guid taskId,
            IBrowserFile file,
            string? versionNote = null,
            CancellationToken cancellationToken = default);
    }
}
