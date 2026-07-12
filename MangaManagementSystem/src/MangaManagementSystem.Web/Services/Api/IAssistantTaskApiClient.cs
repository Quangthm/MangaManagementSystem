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

        // Completed work summary
        Task<AssistantCompletedWorkSummaryDto?> GetCompletedWorkAsync(Guid actorUserId, CancellationToken cancellationToken = default);
        // Submit operation
        Task<AssistantTaskSubmitResultDto?> SubmitTaskWorkAsync(
            Guid actorUserId,
            Guid taskId,
            IBrowserFile file,
            string? versionNote = null,
            CancellationToken cancellationToken = default);

        // Submit from canvas export (base64 image data)
        Task<AssistantTaskSubmitResultDto?> SubmitFromCanvasAsync(
            Guid actorUserId,
            Guid taskId,
            string imageBase64,
            string? versionNote = null,
            CancellationToken cancellationToken = default);
    }
}
