using System.Text.Json.Serialization;

namespace MangaManagementSystem.Application.DTOs.AI;

public class AiRegionDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("originalText")]
    public string? OriginalText { get; set; }

    [JsonPropertyName("translatedText")]
    public string? TranslatedText { get; set; }
}

public class SegmentResponseDto
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("regions")]
    public List<AiRegionDto> Regions { get; set; } = new();
}

public class TranslateRequestDto
{
    [JsonPropertyName("image_base64")]
    public string ImageBase64 { get; set; } = string.Empty;

    [JsonPropertyName("regions")]
    public List<AiRegionDto> Regions { get; set; } = new();
}

public class TranslateResponseDto
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("clean_image_base64")]
    public string? CleanImageBase64 { get; set; }

    [JsonPropertyName("regions")]
    public List<AiRegionDto> Regions { get; set; } = new();
}
