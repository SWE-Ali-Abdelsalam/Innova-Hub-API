using InnoHub.MLService;
using InnoHub.ModelDTO.ML;
using Microsoft.Extensions.Options;

namespace InnoHub.BackgroundServices
{
    public class FlaskHealthMonitorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<FlaskHealthMonitorService> _logger;
        private readonly FlaskAIConfiguration _config;
        private readonly MLFeaturesConfiguration _mlConfig;

        public FlaskHealthMonitorService(
            IServiceProvider serviceProvider,
            ILogger<FlaskHealthMonitorService> logger,
            IOptions<FlaskAIConfiguration> config,
            IOptions<MLFeaturesConfiguration> mlConfig)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _config = config.Value;
            _mlConfig = mlConfig.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Flask Health Monitor Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var recommendationService = scope.ServiceProvider.GetRequiredService<IMLRecommendationService>();

                    var isHealthy = await recommendationService.IsServiceAvailableAsync();

                    if (isHealthy)
                    {
                        _logger.LogDebug("✅ Flask ML API health check passed");
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Flask ML API health check failed - Service may be unavailable");

                        // If Flask is required and fails, you could implement alerts here
                        if (_config.RequiredForOperation)
                        {
                            _logger.LogError("❌ CRITICAL: Flask ML API is required but unavailable!");
                            // Could send notifications to admins here
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during Flask health monitoring");
                }

                // Check every 30 seconds
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }

            _logger.LogInformation("Flask Health Monitor Service stopped");
        }
    }
}