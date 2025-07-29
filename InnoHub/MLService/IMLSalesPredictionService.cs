using InnoHub.ModelDTO.ML;

namespace InnoHub.MLService
{
    public interface IMLSalesPredictionService : IMLBaseService
    {
        Task<SalesPredictionResponseDTO?> PredictSalesAsync(SalesPredictionRequestDTO request);
        Task<SalesPredictionResponseDTO?> PredictProductSalesAsync(int productId, SalesPredictionRequestDTO request);
        Task<SalesPredictionResponseDTO?> PredictDealRevenueAsync(int dealId, SalesPredictionRequestDTO request);
        Task<List<SalesPredictionResponseDTO>> GetSalesForecastAsync(string businessOwnerId);
    }
}
