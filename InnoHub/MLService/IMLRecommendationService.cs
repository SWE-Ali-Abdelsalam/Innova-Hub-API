using InnoHub.Core.Models;
using InnoHub.ModelDTO.ML;

namespace InnoHub.MLService
{
    public interface IMLRecommendationService : IMLBaseService
    {
        Task<RecommendationResponseDTO?> GetRecommendationsAsync(int customerId);
        Task<List<Product>> GetRecommendedProductsAsync(int customerId);
        Task<List<Product>> GetRecommendedProductsForCartAsync(string userId);
        Task<List<Product>> GetRecommendedProductsForHomePageAsync(string? userId = null);
    }
}
