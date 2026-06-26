using System.Net.Http.Headers;
using System.Text.Json;
using MangaManagementSystem.Application.DTOs.AI;
using MangaManagementSystem.Application.Interfaces;

namespace MangaManagementSystem.Infrastructure.Services;

public class AiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "http://127.0.0.1:8000/api/ai";

    public AiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<SegmentResponseDto?> SegmentImageAsync(byte[] imageBytes, string fileName, string contentType)
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(imageBytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        content.Add(fileContent, "file", fileName);

        var response = await _httpClient.PostAsync($"{_baseUrl}/segment", content);
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<SegmentResponseDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        return null;
    }

    public async Task<TranslateResponseDto?> TranslateRegionsAsync(TranslateRequestDto request)
    {
        var jsonContent = JsonSerializer.Serialize(request);
        var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_baseUrl}/translate-selected", content);
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TranslateResponseDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        return null;
    }
}
