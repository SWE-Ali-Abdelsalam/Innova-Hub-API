using InnoHub.Core.Models;
using InnoHub.ModelDTO.ML;
using InnoHub.UnitOfWork;

namespace InnoHub.MLService
{
    public class MLDataMappingService : IMLDataMappingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MLDataMappingService> _logger;

        public MLDataMappingService(IUnitOfWork unitOfWork, ILogger<MLDataMappingService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        #region User Profile Mapping for Spam Detection

        public SpamDetectionRequestDTO MapUserToSpamDetection(AppUser user)
        {
            return new SpamDetectionRequestDTO
            {
                ProfileCompleteness = CalculateProfileCompleteness(user),
                SalesConsistency = DetermineSalesConsistency(user),
                CustomerFeedback = CalculateCustomerFeedback(user),
                TransactionHistory = 0.1,
                PlatformInteraction = DeterminePlatformInteraction(user)
            };
        }

        public double CalculateProfileCompleteness(AppUser user)
        {
            var completionScore = 0.0;
            var totalFields = 10.0;

            // Basic profile fields (5 fields)
            if (!string.IsNullOrEmpty(user.FirstName)) completionScore += 1;
            if (!string.IsNullOrEmpty(user.LastName)) completionScore += 1;
            if (!string.IsNullOrEmpty(user.Email)) completionScore += 1;
            if (!string.IsNullOrEmpty(user.PhoneNumber)) completionScore += 1;
            if (!string.IsNullOrEmpty(user.City)) completionScore += 1;

            // Additional profile fields (3 fields)
            if (!string.IsNullOrEmpty(user.District)) completionScore += 1;
            if (!string.IsNullOrEmpty(user.Country)) completionScore += 1;
            if (user.ProfileImageUrl != "/ProfileImages/DefaultImage.png") completionScore += 1;

            // Verification fields (2 fields)
            if (user.IsIdCardVerified) completionScore += 1;
            if (user.IsSignatureVerified) completionScore += 1;

            return Math.Round(completionScore / totalFields, 2);
        }

        public string DetermineSalesConsistency(AppUser user)
        {
            try
            {
                // Get user's products and their sales history
                var userProducts = _unitOfWork.Product.GetAllAsync().Result
                    .Where(p => p.AuthorId == user.Id).ToList();

                if (!userProducts.Any())
                    return "low"; // No products = low consistency

                var totalProducts = userProducts.Count;
                var productsWithSales = 0;
                var totalSalesVolume = 0;

                foreach (var product in userProducts)
                {
                    // Check if product has sales (you might need to implement this based on your OrderItem logic)
                    var hasSales = CheckProductHasSales(product.Id);
                    if (hasSales)
                    {
                        productsWithSales++;
                        totalSalesVolume += GetProductSalesVolume(product.Id);
                    }
                }

                var salesRatio = (double)productsWithSales / totalProducts;
                var avgSalesPerProduct = totalProducts > 0 ? (double)totalSalesVolume / totalProducts : 0;

                // Determine consistency based on ratio and volume
                if (salesRatio >= 0.7 && avgSalesPerProduct >= 10)
                    return "high";
                else if (salesRatio >= 0.4 && avgSalesPerProduct >= 5)
                    return "medium";
                else
                    return "low";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating sales consistency for user {UserId}", user.Id);
                return "low";
            }
        }

        public double CalculateCustomerFeedback(AppUser user)
        {
            try
            {
                // Get all ratings for user's products
                var userProducts = _unitOfWork.Product.GetAllAsync().Result
                    .Where(p => p.AuthorId == user.Id).ToList();

                if (!userProducts.Any())
                    return 0.5; // Neutral score for users with no products

                var allRatings = new List<double>();
                foreach (var product in userProducts)
                {
                    var productRatings = product.Ratings?.Select(r => (double)r.RatingValue) ?? new List<double>();
                    allRatings.AddRange(productRatings);
                }

                if (!allRatings.Any())
                    return 0.5; // Neutral score if no ratings

                var averageRating = allRatings.Average();
                return Math.Round(averageRating / 5.0, 2); // Normalize to 0-1 scale
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating customer feedback for user {UserId}", user.Id);
                return 0.5;
            }
        }

        public string DeterminePlatformInteraction(AppUser user)
        {
            try
            {
                var score = 0;

                // Check recent login activity
                if (user.LastLoginedAt.HasValue)
                {
                    var daysSinceLogin = (DateTime.UtcNow - user.LastLoginedAt.Value).TotalDays;
                    if (daysSinceLogin <= 7) score += 3;
                    else if (daysSinceLogin <= 30) score += 2;
                    else if (daysSinceLogin <= 90) score += 1;
                }

                // Check if user has products
                var hasProducts = _unitOfWork.Product.GetAllAsync().Result.Any(p => p.AuthorId == user.Id);
                if (hasProducts) score += 2;

                // Check if user has deals
                var hasDeals = _unitOfWork.Deal.GetAllAsync().Result.Any(d => d.AuthorId == user.Id || d.InvestorId == user.Id);
                if (hasDeals) score += 2;

                // Check if user has orders
                var hasOrders = _unitOfWork.Order.GetAllAsync().Result.Any(o => o.UserId == user.Id);
                if (hasOrders) score += 1;

                // Determine interaction level
                return score switch
                {
                    >= 6 => "high",
                    >= 3 => "medium",
                    _ => "low"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining platform interaction for user {UserId}", user.Id);
                return "low";
            }
        }

        #endregion

        #region Product Mapping for Sales Prediction

        public SalesPredictionRequestDTO MapProductToSalesPrediction(Product product, decimal adBudget = 0, string marketingChannel = "Direct")
        {
            return new SalesPredictionRequestDTO
            {
                ProductType = MapProductToFlaskCategory(product),
                Season = GetCurrentSeason(),
                MarketingChannel = marketingChannel,
                AdBudget = (double)adBudget,
                UnitPrice = (double)product.Price,
                UnitsSold = GetProductSalesVolume(product.Id)
            };
        }

        public string MapProductToFlaskCategory(Product product)
        {
            // Map your categories to Flask expected categories
            var categoryName = product.Category?.Name?.ToLower() ?? "";

            return categoryName switch
            {
                var c when c.Contains("laptop") || c.Contains("computer") => "Laptop",
                var c when c.Contains("phone") || c.Contains("mobile") => "Smartphone",
                var c when c.Contains("camera") || c.Contains("photography") => "Camera",
                var c when c.Contains("headphone") || c.Contains("audio") => "Headphones",
                var c when c.Contains("tv") || c.Contains("television") => "TV",
                var c when c.Contains("tablet") || c.Contains("ipad") => "Tablet",
                var c when c.Contains("watch") || c.Contains("smartwatch") => "Watch",
                _ => "Laptop" // Default fallback
            };
        }

        public string GetCurrentSeason()
        {
            var month = DateTime.Now.Month;
            return month switch
            {
                12 or 1 or 2 => "Winter",
                3 or 4 or 5 => "Spring",
                6 or 7 or 8 => "Summer",
                9 or 10 or 11 => "Fall",
                _ => "Spring"
            };
        }

        #endregion

        #region Recommendation Mapping

        public async Task<List<Product>> MapRecommendationsToProducts(List<RecommendationItemDTO> recommendations)
        {
            if (!recommendations.Any())
                return new List<Product>();

            var productIds = recommendations.Select(r => r.ItemId).ToList();
            var products = await _unitOfWork.Product.GetProductsByIdsAsync(productIds);

            // Sort products based on recommendation order
            var sortedProducts = new List<Product>();
            foreach (var recommendation in recommendations)
            {
                var product = products.FirstOrDefault(p => p.Id == recommendation.ItemId);
                if (product != null)
                {
                    sortedProducts.Add(product);
                }
            }

            return sortedProducts;
        }

        #endregion

        #region Helper Methods

        private bool CheckProductHasSales(int productId)
        {
            try
            {
                // Check if this product appears in any orders
                return _unitOfWork.OrderItem.GetAllAsync().Result
                    .Any(oi => oi.ProductId == productId);
            }
            catch
            {
                return false;
            }
        }

        private int GetProductSalesVolume(int productId)
        {
            try
            {
                return _unitOfWork.OrderItem.GetAllAsync().Result
                    .Where(oi => oi.ProductId == productId)
                    .Sum(oi => oi.Quantity);
            }
            catch
            {
                return 0;
            }
        }

        #endregion
    }
}