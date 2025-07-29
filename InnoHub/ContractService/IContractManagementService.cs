//using InnoHub.Core.Models;

//namespace InnoHub.ContractService
//{
//    public interface IContractManagementService
//    {
//        Task<ContractGenerationResult> GenerateContractAsync(Deal deal, ContractType contractType, string? reason = null);
//        Task<ContractValidationResult> ValidateContractAsync(Deal deal);
//        Task<List<ContractHistoryItem>> GetContractHistoryAsync(int dealId);
//        Task<byte[]> GetContractPdfAsync(string contractUrl);
//        Task<string> GetContractHashAsync(Deal deal);
//        Task<bool> ArchiveOldContractAsync(Deal deal);
//        Task<ContractComparisonResult> CompareContractVersionsAsync(int dealId, int version1, int version2);
//        Task NotifyPartiesAboutNewContractAsync(Deal deal, ContractType contractType);
//    }
//}
