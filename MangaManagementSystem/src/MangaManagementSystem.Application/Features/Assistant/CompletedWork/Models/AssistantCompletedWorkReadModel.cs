namespace MangaManagementSystem.Application.Features.Assistant.CompletedWork.Models;

/// <summary>
/// Lightweight read model returned by the repository. No navigation property
/// graph — just flat data ready for the handler to aggregate.
/// </summary>
public sealed record AssistantCompletedWorkReadModel(
    IReadOnlyList<AssistantCompletedTaskRow> Tasks);
