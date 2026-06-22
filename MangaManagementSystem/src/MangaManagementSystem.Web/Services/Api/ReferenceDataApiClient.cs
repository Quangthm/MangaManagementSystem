using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using MangaManagementSystem.Application.DTOs.Manga;
using Microsoft.Extensions.Logging;

namespace MangaManagementSystem.Web.Services.Api
{
    public class ReferenceDataApiClient : IReferenceDataApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ReferenceDataApiClient> _logger;

        public ReferenceDataApiClient(HttpClient httpClient, ILogger<ReferenceDataApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<IReadOnlyList<GenreDto>> GetGenresAsync(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync("api/reference/genres", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var genres = await response.Content.ReadFromJsonAsync<List<GenreDto>>(cancellationToken: cancellationToken);
                return genres ?? new List<GenreDto>();
            }

            _logger.LogWarning("Load reference genres failed: {StatusCode} {ReasonPhrase}",
                (int)response.StatusCode, response.ReasonPhrase);

            throw new InvalidOperationException("We could not load the genre list right now. Please try again later.");
        }

        public async Task<IReadOnlyList<TagDto>> GetTagsAsync(CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.GetAsync("api/reference/tags", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var tags = await response.Content.ReadFromJsonAsync<List<TagDto>>(cancellationToken: cancellationToken);
                return tags ?? new List<TagDto>();
            }

            _logger.LogWarning("Load reference tags failed: {StatusCode} {ReasonPhrase}",
                (int)response.StatusCode, response.ReasonPhrase);

            throw new InvalidOperationException("We could not load the tag list right now. Please try again later.");
        }
    }
}
