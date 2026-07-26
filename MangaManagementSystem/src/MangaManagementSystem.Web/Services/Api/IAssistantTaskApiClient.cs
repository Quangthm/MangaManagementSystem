using MangaManagementSystem.Application.DTOs.Manga;
using Microsoft.AspNetCore.Components.Forms;

namespace MangaManagementSystem.Web.Services.Api
{
    public interface IAssistantTaskApiClient
    {
        Task<IReadOnlyList<ChapterPageTaskDto>>
            GetAssignedTasksAsync(
                CancellationToken cancellationToken = default);

        Task<ChapterPageTaskDto?>
            GetTaskDetailAsync(
                Guid taskId,
                CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ChapterPageAnnotationDto>>
            GetTaskAnnotationsAsync(
                Guid taskId,
                CancellationToken cancellationToken = default);

        Task<AssistantCompletedWorkSummaryDto?>
            GetCompletedWorkAsync(
                CancellationToken cancellationToken = default);

        Task<AssistantTaskSubmitResultDto?>
            SubmitTaskWorkAsync(
                Guid taskId,
                IBrowserFile file,
                string? versionNote = null,
                CancellationToken cancellationToken = default);

        Task<AssistantTaskSubmitResultDto?>
            SubmitTaskWorkAsync(
                Guid taskId,
                byte[] fileBytes,
                string fileName,
                string contentType,
                string? versionNote = null,
                CancellationToken cancellationToken = default);
    }
}
