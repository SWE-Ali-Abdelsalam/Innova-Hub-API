using InnoHub.Core.Models;
using InnoHub.ModelDTO.ML;

namespace InnoHub.MLService
{
    public interface IMLSpamDetectionService : IMLBaseService
    {
        Task<SpamDetectionResponseDTO?> DetectSpamAsync(SpamDetectionRequestDTO request);
        Task<SpamDetectionResponseDTO?> AnalyzeUserAsync(string userId);
        Task<SpamDetectionResponseDTO?> AnalyzeDealAsync(int dealId);
        Task<bool> IsUserSuspiciousAsync(string userId);
        Task<SpamDetectionRequestDTO> BuildUserProfileAsync(AppUser user);
    }
}
