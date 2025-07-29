using InnoHub.Core.Models;
using InnoHub.ModelDTO.ML;

namespace InnoHub.MLService
{
    public interface IMLDataMappingService
    {
        // User Profile Mapping
        SpamDetectionRequestDTO MapUserToSpamDetection(AppUser user);
        double CalculateProfileCompleteness(AppUser user);
        string DetermineSalesConsistency(AppUser user);
        double CalculateCustomerFeedback(AppUser user);
        string DeterminePlatformInteraction(AppUser user);

        // Product Mapping
        SalesPredictionRequestDTO MapProductToSalesPrediction(Product product, decimal adBudget = 0, string marketingChannel = "Direct");
        string MapProductToFlaskCategory(Product product);
        string GetCurrentSeason();

        // Recommendation Mapping
        Task<List<Product>> MapRecommendationsToProducts(List<RecommendationItemDTO> recommendations);
    }
}
