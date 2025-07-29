using InnoHub.ModelDTO.ML;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace InnoHub.MLService
{
    public abstract class MLBaseService : IMLBaseService
    {
        protected readonly HttpClient _httpClient;
        protected readonly FlaskAIConfiguration _config;
        protected readonly ILogger _logger;

        protected MLBaseService(HttpClient httpClient, IOptions<FlaskAIConfiguration> config, ILogger logger)
        {
            _httpClient = httpClient;
            _config = config.Value;
            _logger = logger;
        }

        public virtual async Task<MLHealthCheckResponseDTO?> CheckHealthAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_config.BaseUrl}{_config.Endpoints.Health}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<MLHealthCheckResponseDTO>(content);
                }

                _logger.LogWarning("ML Health check failed with status: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking ML service health");
                return null;
            }
        }

        public virtual async Task<bool> IsServiceAvailableAsync()
        {
            var health = await CheckHealthAsync();
            return health?.StatusCode == 200 && health.Status == "healthy";
        }

        protected async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest request)
            where TResponse : class
        {
            try
            {
                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_config.BaseUrl}{endpoint}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<TResponse>(responseContent);
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("ML API call failed. Endpoint: {Endpoint}, Status: {StatusCode}, Error: {Error}",
                    endpoint, response.StatusCode, errorContent);

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling ML API endpoint: {Endpoint}", endpoint);
                return null;
            }
        }
    }
}
