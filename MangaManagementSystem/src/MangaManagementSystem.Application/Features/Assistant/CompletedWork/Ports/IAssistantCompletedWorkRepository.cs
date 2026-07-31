using MangaManagementSystem.Application.Features.Assistant.CompletedWork.Models;

namespace MangaManagementSystem.Application.Features.Assistant.CompletedWork.Ports;

public interface IAssistantCompletedWorkRepository
{
    /// <summary>
    /// Returns completed tasks for the given assistant, with page-region context
    /// (Series title, chapter title, page number) pre-joined for display.
    /// </summary>
    Task<AssistantCompletedWorkReadModel> GetCompletedWorkAsync(
        Guid assistantUserId,
        CancellationToken cancellationToken = default);
}
