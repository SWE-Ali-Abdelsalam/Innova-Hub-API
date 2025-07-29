using InnoHub.MLService;
using InnoHub.ModelDTO.ML;
using Microsoft.Extensions.Options;

namespace InnoHub.Middleware
{
    public class FlaskHealthCheckMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<FlaskHealthCheckMiddleware> _logger;
        private readonly FlaskAIConfiguration _config;
        private readonly MLFeaturesConfiguration _mlConfig;

        public FlaskHealthCheckMiddleware(
            RequestDelegate next,
            ILogger<FlaskHealthCheckMiddleware> logger,
            IOptions<FlaskAIConfiguration> config,
            IOptions<MLFeaturesConfiguration> mlConfig)
        {
            _next = next;
            _logger = logger;
            _config = config.Value;
            _mlConfig = mlConfig.Value;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only check Flask health for ML-related endpoints
            if (ShouldCheckFlaskHealth(context.Request.Path))
            {
                if (_config.RequiredForOperation && _mlConfig.RequireFlaskForAll)
                {
                    var flaskHealthy = await CheckFlaskHealthAsync(context);
                    if (!flaskHealthy)
                    {
                        _logger.LogError("Flask ML API is required but unavailable for path: {Path}", context.Request.Path);

                        context.Response.StatusCode = 503; // Service Unavailable
                        context.Response.ContentType = "application/json";

                        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
                        {
                            Message = "ML services are currently unavailable",
                            Error = "Flask AI/ML API is not responding",
                            StatusCode = 503,
                            Source = "Flask Health Check Middleware",
                            FlaskURL = _config.BaseUrl,
                            Timestamp = DateTime.UtcNow
                        }));
                        return;
                    }
                }
            }

            await _next(context);
        }

        private bool ShouldCheckFlaskHealth(string path)
        {
            var mlPaths = new[]
            {
                "/api/ml/",
                "/api/product/recommendations/",
                "/api/cart/recommendations",
                // Add other paths that depend on ML
            };

            return mlPaths.Any(mlPath => path.StartsWith(mlPath, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<bool> CheckFlaskHealthAsync(HttpContext context)
        {
            try
            {
                // Get ML service from DI container
                var recommendationService = context.RequestServices.GetService<IMLRecommendationService>();
                if (recommendationService == null) return false;

                return await recommendationService.IsServiceAvailableAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Flask health in middleware");
                return false;
            }
        }
    }
}