using MangaManagementSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MangaManagementSystem.Domain.Interfaces
{
    public interface IAssistantCompletedWorkRepository
    {
        /// <summary>
        /// Returns completed tasks for the given assistant, with page-region context
        /// (Series title, chapter title, page number) pre-joined for display.
        /// </summary>
        Task<AssistantCompletedWorkReadModel> GetCompletedWorkAsync(
            Guid assistantUserId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Lightweight read model returned by the repository. No navigation property
    /// graph — just flat data ready for the handler to aggregate.
    /// </summary>
    public sealed record AssistantCompletedWorkReadModel(
        IReadOnlyList<AssistantCompletedTaskRow> Tasks);
}
