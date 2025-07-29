using InnoHub.Core.Models;
using InnoHub.ModelDTO.ML;
using Microsoft.Extensions.Options;

namespace InnoHub.MLService
{
    public class MLRecommendationService : MLBaseService, IMLRecommendationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly MLFeaturesConfiguration _mlConfig;

        public MLRecommendationService(
            HttpClient httpClient,
            IOptions<FlaskAIConfiguration> config,
            IOptions<MLFeaturesConfiguration> mlConfig,
            IUnitOfWork unitOfWork,
            ILogger<MLRecommendationService> logger)
            : base(httpClient, config, logger)
        {
            _unitOfWork = unitOfWork;
            _mlConfig = mlConfig.Value;
        }

        public async Task<RecommendationResponseDTO?> GetRecommendationsAsync(int customerId)
        {
            if (!_mlConfig.EnableRecommendations)
            {
                throw new InvalidOperationException("Recommendations are disabled in configuration");
            }

            var request = new RecommendationRequestDTO { CustomerId = customerId };
            var response = await PostAsync<RecommendationRequestDTO, RecommendationResponseDTO>(
                _config.Endpoints.Recommend, request);

            // ❌ NO LOCAL FALLBACK - FLASK ONLY
            if (response == null)
            {
                throw new ApplicationException("Flask ML API failed to provide recommendations. Service unavailable.");
            }

            return response;
        }

        public async Task<List<Product>> GetRecommendedProductsAsync(int customerId)
        {
            // ✅ FLASK ONLY - NO FALLBACKS
            var recommendations = await GetRecommendationsAsync(customerId);

            if (recommendations?.Recommendations == null || !recommendations.Recommendations.Any())
            {
                throw new ApplicationException($"Flask ML API returned no recommendations for customer {customerId}");
            }

            // Extract product IDs from Flask recommendations
            var productIds = recommendations.Recommendations
                .Select(r => r.ItemId)
                .ToList();

            // Fetch actual products from database based on Flask recommendations
            var products = await _unitOfWork.Product.GetProductsByIdsAsync(productIds);

            if (!products.Any())
            {
                throw new ApplicationException("Flask recommended products not found in database");
            }

            // Enrich products with Flask recommendation data
            foreach (var product in products)
            {
                var recommendation = recommendations.Recommendations
                    .FirstOrDefault(r => r.ItemId == product.Id);

                if (recommendation != null)
                {
                    // Store Flask recommendation metadata
                    product.TotalSold = (int)(recommendation.PredictedRating ?? recommendation.Score ?? 0);
                }
            }

            return products.Take(_mlConfig.RecommendationSettings.MaxRecommendations).ToList();
        }

        public async Task<List<Product>> GetRecommendedProductsForCartAsync(string userId)
        {
            // Convert userId to customerId for Flask API
            int customerIdInt;
            if (!int.TryParse(userId, out customerIdInt))
            {
                // Use hash-based approach for non-numeric userIds
                customerIdInt = Math.Abs(userId.GetHashCode()) % 100000 + 1;
            }

            // ✅ COMPLETELY DEPENDS ON FLASK
            return await GetRecommendedProductsAsync(customerIdInt);
        }

        public async Task<List<Product>> GetRecommendedProductsForHomePageAsync(string? userId = null)
        {
            if (string.IsNullOrEmpty(userId))
            {
                // ❌ NO LOCAL FALLBACK - STILL USE FLASK
                // Use a default customer ID for anonymous users
                return await GetRecommendedProductsAsync(1); // Flask will handle this
            }

            return await GetRecommendedProductsForCartAsync(userId);
        }
    }
}
