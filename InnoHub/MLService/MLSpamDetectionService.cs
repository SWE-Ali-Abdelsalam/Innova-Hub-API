using InnoHub.Core.Models;
using InnoHub.ModelDTO.ML;
using Microsoft.Extensions.Options;

namespace InnoHub.MLService
{
    public class MLSpamDetectionService : MLBaseService, IMLSpamDetectionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMLDataMappingService _mappingService;
        private readonly MLFeaturesConfiguration _mlConfig;

        public MLSpamDetectionService(
            HttpClient httpClient,
            IOptions<FlaskAIConfiguration> config,
            IOptions<MLFeaturesConfiguration> mlConfig,
            IUnitOfWork unitOfWork,
            IMLDataMappingService mappingService,
            ILogger<MLSpamDetectionService> logger)
            : base(httpClient, config, logger)
        {
            _unitOfWork = unitOfWork;
            _mappingService = mappingService;
            _mlConfig = mlConfig.Value;
        }

        public async Task<SpamDetectionResponseDTO?> DetectSpamAsync(SpamDetectionRequestDTO request)
        {
            if (!_mlConfig.EnableSpamDetection)
            {
                throw new InvalidOperationException("Spam detection is disabled in configuration");
            }

            var response = await PostAsync<SpamDetectionRequestDTO, SpamDetectionResponseDTO>(
                _config.Endpoints.SpamDetection, request);

            // ❌ NO LOCAL FALLBACK - FLASK ONLY
            if (response == null)
            {
                throw new ApplicationException("Flask ML API failed to detect spam. Service unavailable.");
            }

            // Add business logic based on Flask response
            response.RecommendedAction = DetermineActionFromFlask(response);
            response.ConfidenceScore = CalculateConfidenceFromFlask(response);

            return response;
        }

        public async Task<SpamDetectionResponseDTO?> AnalyzeUserAsync(string userId)
        {
            var user = await _unitOfWork.AppUser.GetUSerByIdAsync(userId);
            if (user == null)
            {
                throw new ArgumentException($"User not found: {userId}");
            }

            var request = await BuildUserProfileAsync(user);

            // ✅ COMPLETELY DEPENDS ON FLASK
            return await DetectSpamAsync(request);
        }

        public async Task<SpamDetectionResponseDTO?> AnalyzeDealAsync(int dealId)
        {
            var deal = await _unitOfWork.Deal.GetDealWithDetails(dealId);
            if (deal?.Author == null)
            {
                throw new ArgumentException($"Deal or author not found: {dealId}");
            }

            // ✅ FLASK ONLY
            return await AnalyzeUserAsync(deal.AuthorId);
        }

        public async Task<bool> IsUserSuspiciousAsync(string userId)
        {
            // ✅ COMPLETELY DEPENDS ON FLASK
            var analysis = await AnalyzeUserAsync(userId);
            return analysis.IsSpam;
        }

        public async Task<SpamDetectionRequestDTO> BuildUserProfileAsync(AppUser user)
        {
            return _mappingService.MapUserToSpamDetection(user);
        }

        private string DetermineActionFromFlask(SpamDetectionResponseDTO response)
        {
            if (response.IsSpam)
            {
                return _mlConfig.SpamDetectionSettings.AutoBlockSpamUsers
                    ? "Auto-block user based on Flask ML prediction"
                    : "Flag for manual review based on Flask ML prediction";
            }
            return "No action required - Flask ML confirmed user is legitimate";
        }

        private double CalculateConfidenceFromFlask(SpamDetectionResponseDTO response)
        {
            // This is based purely on Flask response analysis
            return response.IsSpam ? 0.85 : 0.95;
        }
    }
}
