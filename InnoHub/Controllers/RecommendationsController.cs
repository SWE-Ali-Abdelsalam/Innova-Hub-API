using InnoHub.Core.Models;
using InnoHub.MLService;
using InnoHub.ModelDTO;
using InnoHub.ModelDTO.ML;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace InnoHub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecommendationsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<RecommendationsController> _logger;
        private readonly MLSalesPredictionService _salesPredictionService;

        public RecommendationsController(
            IUnitOfWork unitOfWork,
            ILogger<RecommendationsController> logger,
            MLSalesPredictionService salesPredictionService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _salesPredictionService = salesPredictionService;
        }

        [HttpPost("predict-sales")]
        public async Task<IActionResult> PredictSales([FromBody] SalesPredictionRequestDTO request)
        {
            try
            {
                var prediction = await _salesPredictionService.PredictSalesAsync(request);

                return Ok(new
                {
                    PredictedRevenue = Math.Round(prediction.PredictedSalesRevenue, 2)
                });
            }
            catch (ApplicationException ex)
            {
                _logger.LogError(ex, "Flask ML API failed for sales prediction");
                return ServiceUnavailable(new
                {
                    Message = "ML Sales Prediction service is currently unavailable",
                    Error = ex.Message,
                    Source = "Flask ML API Error"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error predicting sales via Flask");
                return StatusCode(500, new { Message = "Error predicting sales", Error = ex.Message });
            }
        }

        #region Private Helper Methods for Personalized Popular Recommendations

        private async Task<UserBehaviorProfile> AnalyzeUserBehaviorAsync(string userId)
        {
            var profile = new UserBehaviorProfile { UserId = userId };

            try
            {
                // 1. تحليل تاريخ المشتريات
                var userOrders = await _unitOfWork.Order.GetAllOrdersForSpecificUser(userId);
                var ordersList = userOrders?.ToList() ?? new List<Order>();

                profile.PurchaseHistory = ordersList.Count;
                profile.IsNewUser = ordersList.Count == 0;

                if (!profile.IsNewUser)
                {
                    // 2. استخراج الفئات المفضلة من المشتريات
                    var purchasedProductIds = ordersList
                        .SelectMany(o => o.OrderItems.Select(oi => oi.ProductId))
                        .ToList();

                    if (purchasedProductIds.Any())
                    {
                        var purchasedProducts = await _unitOfWork.Product.GetProductsByIdsAsync(purchasedProductIds);
                        profile.FavoriteCategories = purchasedProducts
                            .GroupBy(p => p.CategoryId)
                            .OrderByDescending(g => g.Count())
                            .Take(3)
                            .Select(g => g.Key)
                            .ToList();
                    }

                    // 3. تحليل النطاق السعري المفضل
                    var totalSpent = ordersList.Sum(o => o.Total);
                    var avgOrderValue = ordersList.Any() ? totalSpent / ordersList.Count : 0;

                    profile.PriceRange = new PriceRange
                    {
                        MinPrice = Math.Max(0, avgOrderValue * 0.5m),
                        MaxPrice = avgOrderValue * 1.5m,
                        AverageSpending = avgOrderValue
                    };
                }

                // 4. تحليل التقييمات
                var userRatings = await GetUserRatingsAsync(userId);
                profile.HighRatedCategories = userRatings
                    .Where(r => r.RatingValue >= 4)
                    .Select(r => r.Product.CategoryId)
                    .GroupBy(cId => cId)
                    .OrderByDescending(g => g.Count())
                    .Take(3)
                    .Select(g => g.Key)
                    .ToList();

                // 5. تحليل الـ Wishlist
                var wishlist = await _unitOfWork.Wishlist.GetWishlistByUserID(userId);
                if (wishlist?.WishlistItems?.Any() == true)
                {
                    profile.WishlistCategories = wishlist.WishlistItems
                        .Where(wi => wi.Product != null)
                        .Select(wi => wi.Product.CategoryId)
                        .GroupBy(cId => cId)
                        .OrderByDescending(g => g.Count())
                        .Take(3)
                        .Select(g => g.Key)
                        .ToList();
                }

                _logger.LogInformation("📊 User behavior analysis completed for {UserId}: Categories={Categories}, Orders={Orders}",
                    userId, profile.FavoriteCategories.Count, profile.PurchaseHistory);

                return profile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing user behavior for {UserId}", userId);
                profile.IsNewUser = true;
                return profile;
            }
        }

        /// <summary>
        /// بناء التوصيات المخصصة بناءً على ملف المستخدم
        /// </summary>
        private async Task<List<Product>> BuildPersonalizedPopularRecommendations(UserBehaviorProfile userProfile, int count)
        {
            var allProducts = await _unitOfWork.Product.GetAllAsync();
            var scoredProducts = new List<(Product Product, double Score)>();

            foreach (var product in allProducts.Where(p => p.Stock > 0))
            {
                double score = CalculatePersonalizationScore(product, userProfile);
                scoredProducts.Add((product, score));
            }

            return scoredProducts
                .OrderByDescending(sp => sp.Score)
                .Take(count)
                .Select(sp => sp.Product)
                .ToList();
        }

        /// <summary>
        /// حساب نقاط التخصيص للمنتج
        /// </summary>
        private double CalculatePersonalizationScore(Product product, UserBehaviorProfile userProfile)
        {
            double score = 0.0;

            // 1. نقاط الفئة المفضلة (40%)
            if (userProfile.FavoriteCategories.Contains(product.CategoryId))
            {
                int categoryRank = userProfile.FavoriteCategories.IndexOf(product.CategoryId);
                score += (0.4 * (3 - categoryRank) / 3.0); // الفئة الأولى تحصل على أعلى نقاط
            }

            // 2. نقاط التقييمات العالية (20%)
            if (userProfile.HighRatedCategories.Contains(product.CategoryId))
            {
                score += 0.2;
            }

            // 3. نقاط الـ Wishlist (15%)
            if (userProfile.WishlistCategories.Contains(product.CategoryId))
            {
                score += 0.15;
            }

            // 4. نقاط النطاق السعري (15%)
            if (userProfile.PriceRange != null)
            {
                var finalPrice = product.Price * (1 - product.Discount / 100);
                if (finalPrice >= userProfile.PriceRange.MinPrice && finalPrice <= userProfile.PriceRange.MaxPrice)
                {
                    score += 0.15;
                }
            }

            // 5. نقاط جودة المنتج (10%)
            score += (product.AverageRating / 5.0) * 0.1;

            return Math.Round(score, 3);
        }

        /// <summary>
        /// الحصول على سبب التوصية باللغة الإنجليزية
        /// </summary>
        private string GetRecommendationReason(Product product, UserBehaviorProfile userProfile)
        {
            var reasons = new List<string>();

            if (userProfile.FavoriteCategories.Contains(product.CategoryId))
            {
                int rank = userProfile.FavoriteCategories.IndexOf(product.CategoryId) + 1;
                reasons.Add($"From your favorite category #{rank}");
            }

            if (userProfile.HighRatedCategories.Contains(product.CategoryId))
            {
                reasons.Add("From a highly rated category");
            }

            if (userProfile.WishlistCategories.Contains(product.CategoryId))
            {
                reasons.Add("Similar to products in your wishlist");
            }

            if (userProfile.PriceRange != null)
            {
                var finalPrice = product.Price * (1 - product.Discount / 100);
                if (finalPrice >= userProfile.PriceRange.MinPrice && finalPrice <= userProfile.PriceRange.MaxPrice)
                {
                    reasons.Add("Within your preferred price range");
                }
            }

            if (product.AverageRating >= 4.0)
            {
                reasons.Add("Highly rated by users");
            }

            return reasons.Any() ? string.Join(" • ", reasons) : "Featured product";
        }

        /// <summary>
        /// الحصول على المنتجات الشعبية العامة للمستخدمين الجدد
        /// </summary>
        private async Task<IActionResult> GetGeneralPopularRecommendations(int count)
        {
            var popularProducts = await _unitOfWork.Product.GetBestSellingProductsAsync(count);

            var response = popularProducts.Select((product, index) => new
            {
                ProductId = product.Id,
                ProductName = product.Name,
                AuthorName = product.Author != null ? $"{product.Author.FirstName} {product.Author.LastName}" : "Unknown",
                Price = product.Price,
                DiscountedPrice = product.Discount > 0 ? product.Price * (1 - product.Discount / 100) : product.Price,
                HomePictureUrl = !string.IsNullOrEmpty(product.HomePicture) ? $"https://innova-hub.premiumasp.net{product.HomePicture}" : null,
                AverageRating = product.AverageRating,
                Stock = product.Stock,
                Category = product.Category?.Name ?? "Unknown",
                PopularityRank = index + 1,
                RecommendationReason = "Popular among users",
                MatchScore = 1.0 - (index * 0.1), // تناقص النقاط حسب الترتيب
                Description = product.Description ?? "",
                CreatedAt = product.CreatedAt
            }).ToList();

            return Ok(new
            {
                Count = response.Count,
                IsNewUser = true,
                Recommendations = response
            });
        }

        private async Task<List<ProductRating>> GetUserRatingsAsync(string userId)
        {
            try
            {
                var ratings = await _unitOfWork.DbContext.ProductRatings
                    .Include(r => r.Product)
                    .Where(r => r.UserId == userId)
                    .ToListAsync();

                return ratings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user ratings for {UserId}", userId);
                return new List<ProductRating>();
            }
        }

        private ObjectResult ServiceUnavailable(object value)
        {
            return StatusCode(503, value); // 503 Service Unavailable
        }

        #endregion
    }

    #region Helper Classes

    /// <summary>
    /// ملف سلوك المستخدم
    /// </summary>
    public class UserBehaviorProfile
    {
        public string UserId { get; set; } = string.Empty;
        public bool IsNewUser { get; set; } = true;
        public List<int> FavoriteCategories { get; set; } = new List<int>();
        public List<int> HighRatedCategories { get; set; } = new List<int>();
        public List<int> WishlistCategories { get; set; } = new List<int>();
        public PriceRange? PriceRange { get; set; }
        public int PurchaseHistory { get; set; } = 0;
    }

    /// <summary>
    /// النطاق السعري المفضل للمستخدم
    /// </summary>
    public class PriceRange
    {
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public decimal AverageSpending { get; set; }
    }

    #endregion
}