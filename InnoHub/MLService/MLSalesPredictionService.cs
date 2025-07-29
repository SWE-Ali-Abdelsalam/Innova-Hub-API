using InnoHub.ModelDTO.ML;
using Microsoft.Extensions.Options;

namespace InnoHub.MLService
{
    public class MLSalesPredictionService : MLBaseService, IMLSalesPredictionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMLDataMappingService _mappingService;
        private readonly MLFeaturesConfiguration _mlConfig;

        public MLSalesPredictionService(
            HttpClient httpClient,
            IOptions<FlaskAIConfiguration> config,
            IOptions<MLFeaturesConfiguration> mlConfig,
            IUnitOfWork unitOfWork,
            IMLDataMappingService mappingService,
            ILogger<MLSalesPredictionService> logger)
            : base(httpClient, config, logger)
        {
            _unitOfWork = unitOfWork;
            _mappingService = mappingService;
            _mlConfig = mlConfig.Value;
        }

        public async Task<SalesPredictionResponseDTO?> PredictSalesAsync(SalesPredictionRequestDTO request)
        {
            if (!_mlConfig.EnableSalesPrediction)
            {
                throw new InvalidOperationException("Sales prediction is disabled in configuration");
            }

            var response = await PostAsync<SalesPredictionRequestDTO, SalesPredictionResponseDTO>(
                _config.Endpoints.SalesPrediction, request);

            // ❌ NO LOCAL FALLBACK - FLASK ONLY
            if (response == null)
            {
                throw new ApplicationException("Flask ML API failed to predict sales. Service unavailable.");
            }

            // Enrich response with business insights based purely on Flask data
            EnrichPredictionResponseFromFlask(response);

            return response;
        }

        public async Task<SalesPredictionResponseDTO?> PredictProductSalesAsync(int productId, SalesPredictionRequestDTO request)
        {
            var product = await _unitOfWork.Product.GetByIdAsync(productId);
            if (product == null)
            {
                throw new ArgumentException($"Product not found: {productId}");
            }

            // Override values from product but still use Flask for prediction
            request.ProductType = _mappingService.MapProductToFlaskCategory(product);
            request.UnitPrice = (double)product.Price;

            // ✅ COMPLETELY DEPENDS ON FLASK
            return await PredictSalesAsync(request);
        }

        public async Task<SalesPredictionResponseDTO?> PredictDealRevenueAsync(int dealId, SalesPredictionRequestDTO request)
        {
            var deal = await _unitOfWork.Deal.GetDealWithDetails(dealId);
            if (deal == null)
            {
                throw new ArgumentException($"Deal not found: {dealId}");
            }

            // Use deal-specific values but Flask for prediction
            request.AdBudget = (double)deal.OfferMoney * 0.1;
            request.UnitPrice = (double)deal.EstimatedPrice;

            // ✅ FLASK ONLY
            return await PredictSalesAsync(request);
        }

        public async Task<List<SalesPredictionResponseDTO>> GetSalesForecastAsync(string businessOwnerId)
        {
            var products = await _unitOfWork.Product.GetAllAsync();
            var ownerProducts = products.Where(p => p.AuthorId == businessOwnerId).ToList();

            if (!ownerProducts.Any())
            {
                throw new ArgumentException($"No products found for business owner: {businessOwnerId}");
            }

            var forecasts = new List<SalesPredictionResponseDTO>();

            foreach (var product in ownerProducts.Take(5)) // Limit to prevent too many API calls
            {
                var request = _mappingService.MapProductToSalesPrediction(product);

                // ✅ EACH PREDICTION DEPENDS ON FLASK
                var prediction = await PredictProductSalesAsync(product.Id, request);
                forecasts.Add(prediction);
            }

            return forecasts;
        }

        private void EnrichPredictionResponseFromFlask(SalesPredictionResponseDTO response)
        {
            // All calculations based purely on Flask prediction
            var flaskRevenue = response.PredictedSalesRevenue;
            var totalCost = response.InputFeatures.AdBudget +
                           (response.InputFeatures.UnitPrice * response.InputFeatures.UnitsSold * 0.7);

            response.ROI = totalCost > 0 ? (flaskRevenue - totalCost) / totalCost * 100 : 0;
            response.ProfitMargin = flaskRevenue > 0
                ? (flaskRevenue - totalCost) / flaskRevenue * 100
                : 0;

            // Performance category based on Flask prediction
            response.PerformanceCategory = response.ROI switch
            {
                >= 50 => "Excellent (Flask ML Prediction)",
                >= 25 => "Good (Flask ML Prediction)",
                >= 10 => "Average (Flask ML Prediction)",
                _ => "Poor (Flask ML Prediction)"
            };

            // Recommendations based on Flask prediction
            response.Recommendations = GenerateRecommendationsFromFlask(response);
        }

        private List<string> GenerateRecommendationsFromFlask(SalesPredictionResponseDTO response)
        {
            var recommendations = new List<string>();

            if (response.ROI < 10)
            {
                recommendations.Add("Flask ML suggests: Reduce advertising budget or improve pricing strategy");
            }

            if (response.InputFeatures.AdBudget < 1000)
            {
                recommendations.Add("Flask ML suggests: Increase marketing budget to improve visibility");
            }

            if (response.ProfitMargin < 20)
            {
                recommendations.Add("Flask ML suggests: Review cost structure to improve profit margins");
            }

            if (response.InputFeatures.MarketingChannel == "Direct")
            {
                recommendations.Add("Flask ML suggests: Diversify marketing channels for better reach");
            }

            return recommendations;
        }
    }
}
