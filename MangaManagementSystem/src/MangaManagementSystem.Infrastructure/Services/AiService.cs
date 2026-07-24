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

        return await SendAsync<SegmentResponseDto>($"{_baseUrl}/segment", content, "detect speech bubbles");
    }

    public async Task<TranslateResponseDto?> TranslateRegionsAsync(TranslateRequestDto request)
    {
        var jsonContent = JsonSerializer.Serialize(request);
        var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

        return await SendAsync<TranslateResponseDto>($"{_baseUrl}/translate-selected", content, "translate regions");
    }

    /// <summary>
    /// Posts to the Python AI microservice and deserializes its response.
    /// </summary>
    /// <remarks>
    /// The AI service is optional and started by hand (see AI_Setup_Guide.md), so "not running" is the
    /// most common failure by far. Previously every failure — service down, HTTP 500, malformed JSON —
    /// collapsed into a null return that the workspace surfaced as "AI returned null", which told the
    /// user nothing about what to do. Each cause now raises a distinct, actionable message; the caller
    /// in the workspace already catches exceptions and shows Message, so no UI change is needed.
    /// </remarks>
    private async Task<T?> SendAsync<T>(string url, HttpContent content, string operation)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync(url, content);
        }
        catch (HttpRequestException ex)
        {
            // Connection refused / DNS / socket error: the service almost certainly is not running.
            throw new InvalidOperationException(
                "AI service is not reachable. Start it on port 8000 (see AI_Setup_Guide.md), then try again.", ex);
        }
        catch (TaskCanceledException ex)
        {
            // HttpClient surfaces its own timeout as TaskCanceledException. Deliberately NOT retried:
            // inference is expensive, so retrying a timeout just doubles the load on a service that is
            // already struggling.
            throw new InvalidOperationException(
                $"AI service timed out while trying to {operation}. It may be busy or loading its models — try again in a moment.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"AI service failed to {operation} (HTTP {(int)response.StatusCode}). Check the AI service console for details.");
        }

        var json = await response.Content.ReadAsStringAsync();
        try
        {
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"AI service returned an unreadable response while trying to {operation}.", ex);
        }
    }
}
