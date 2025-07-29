using InnoHub.ModelDTO.ML;

namespace InnoHub.MLService
{
    public interface IMLBaseService
    {
        Task<MLHealthCheckResponseDTO?> CheckHealthAsync();
        Task<bool> IsServiceAvailableAsync();
    }
}
