using MangaManagementSystem.Application.DTOs.AI;

namespace MangaManagementSystem.Application.Interfaces;

public interface IAiService
{
    Task<SegmentResponseDto?> SegmentImageAsync(byte[] imageBytes, string fileName, string contentType);
    Task<TranslateResponseDto?> TranslateRegionsAsync(TranslateRequestDto request);
}
