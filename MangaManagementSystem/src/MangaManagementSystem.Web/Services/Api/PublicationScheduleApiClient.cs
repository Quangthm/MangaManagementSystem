using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.Features.Publication.Schedule.Queries.GetPublicationScheduleCalendar;
using MangaManagementSystem.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.Web.Services.Api
{
    public class PublicationScheduleApiClient : IPublicationScheduleApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PublicationScheduleApiClient> _logger;

        public PublicationScheduleApiClient(HttpClient httpClient, ILogger<PublicationScheduleApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<PublicationScheduleCalendarDto> GetScheduleAsync(
            DateTime? anchorDate = null,
            Guid? seriesId = null,
            string? frequencyCode = null,
            CancellationToken cancellationToken = default)
        {
            var sb = new StringBuilder("api/publication/schedule?");
            var hasParam = false;

            if (anchorDate.HasValue)
            {
                sb.Append($"anchorDate={anchorDate.Value:yyyy-MM-dd}");
                hasParam = true;
            }

            if (seriesId.HasValue && seriesId.Value != Guid.Empty)
            {
                if (hasParam) sb.Append('&');
                sb.Append($"seriesId={seriesId.Value}");
                hasParam = true;
            }

            if (!string.IsNullOrWhiteSpace(frequencyCode))
            {
                if (hasParam) sb.Append('&');
                sb.Append($"frequencyCode={Uri.EscapeDataString(frequencyCode)}");
            }

            var response = await _httpClient.GetAsync(sb.ToString(), cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PublicationScheduleCalendarDto>(
                    cancellationToken: cancellationToken);

                if (result is null)
                {
                    throw new InvalidOperationException(
                        "The publication schedule returned no data. Please refresh and try again.");
                }

                return result;
            }

            var errorText = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Load publication schedule failed: {StatusCode}", (int)response.StatusCode);
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(errorText)
                    ? "Failed to load the publication schedule."
                    : errorText);
        }

        public async Task<IReadOnlyList<PublicationScheduleSeriesSuggestion>> GetSeriesSuggestionsAsync(
            string searchText,
            CancellationToken cancellationToken = default)
        {
            var url = $"api/publication/schedule/series-suggestions?searchText={Uri.EscapeDataString(searchText)}";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<PublicationScheduleSeriesSuggestion>>(
                    cancellationToken: cancellationToken);

                return result ?? (IReadOnlyList<PublicationScheduleSeriesSuggestion>)Array.Empty<PublicationScheduleSeriesSuggestion>();
            }

            return Array.Empty<PublicationScheduleSeriesSuggestion>();
        }

        public async Task<PublicationScheduleSeriesSuggestion?> GetSeriesSuggestionBySlugAsync(
            string slug,
            CancellationToken cancellationToken = default)
        {
            var url = $"api/publication/schedule/series/by-slug/{Uri.EscapeDataString(slug)}";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PublicationScheduleSeriesSuggestion>(
                    cancellationToken: cancellationToken);
            }

            return null;
        }

        public async Task<PublicationScheduleSeriesSuggestion?> GetSeriesSuggestionByIdAsync(
            Guid seriesId,
            CancellationToken cancellationToken = default)
        {
            var url = $"api/publication/schedule/series/by-id/{seriesId}";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PublicationScheduleSeriesSuggestion>(
                    cancellationToken: cancellationToken);
            }

            return null;
        }
    }
}
