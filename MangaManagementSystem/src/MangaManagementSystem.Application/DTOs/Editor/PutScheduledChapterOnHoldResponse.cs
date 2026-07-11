using System;

namespace MangaManagementSystem.Application.DTOs.Editor;

public sealed record PutScheduledChapterOnHoldResponse(
    Guid ChapterId,
    string StatusCode,
    string Message);
