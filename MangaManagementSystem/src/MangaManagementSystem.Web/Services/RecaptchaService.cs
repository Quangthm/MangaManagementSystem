using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using MangaManagementSystem.Web.Options;

namespace MangaManagementSystem.Web.Services
{
    public class RecaptchaService
    {
        private readonly HttpClient _httpClient;
        private readonly RecaptchaOptions _options;
        private readonly ILogger<RecaptchaService> _logger;

        public RecaptchaService(HttpClient httpClient, IOptions<RecaptchaOptions> options, ILogger<RecaptchaService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public string SiteKey => _options.SiteKey;

        public async Task<bool> VerifyTokenAsync(string token, string? remoteIp = null)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("reCAPTCHA verification failed: Token is empty.");
                return false;
            }

            try
            {
                var parameters = new Dictionary<string, string>
                {
                    { "secret", _options.SecretKey },
                    { "response", token }
                };

                if (!string.IsNullOrEmpty(remoteIp))
                {
                    parameters.Add("remoteip", remoteIp);
                }

                var content = new FormUrlEncodedContent(parameters);
                var response = await _httpClient.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("reCAPTCHA API request failed with status code {StatusCode}", response.StatusCode);
                    return false;
                }

                var jsonString = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<RecaptchaResponse>(jsonString);

                if (result == null)
                {
                    _logger.LogError("Failed to deserialize reCAPTCHA response.");
                    return false;
                }

                if (!result.Success)
                {
                    _logger.LogWarning("reCAPTCHA verification failed. Error codes: {ErrorCodes}", 
                        result.ErrorCodes != null ? string.Join(", ", result.ErrorCodes) : "none");
                }

                return result.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during reCAPTCHA verification.");
                return false;
            }
        }

        private class RecaptchaResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }

            [JsonPropertyName("challenge_ts")]
            public string ChallengeTs { get; set; } = string.Empty;

            [JsonPropertyName("hostname")]
            public string Hostname { get; set; } = string.Empty;

            [JsonPropertyName("error-codes")]
            public List<string>? ErrorCodes { get; set; }
        }
    }
}
