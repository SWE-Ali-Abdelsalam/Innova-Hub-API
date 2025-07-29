//using InnoHub.Core.Models;
//using InnoHub.Service.FileService;
//using iTextSharp.text;
//using iTextSharp.text.pdf;
//using System.Text;

//namespace InnoHub.ContractService
//{
//    public class ContractManagementService : IContractManagementService
//    {
//        private readonly IUnitOfWork _unitOfWork;
//        private readonly IFileService _fileService;
//        private readonly IWebHostEnvironment _environment;
//        private readonly ILogger<ContractManagementService> _logger;

//        public ContractManagementService(
//            IUnitOfWork unitOfWork,
//            IFileService fileService,
//            IWebHostEnvironment environment,
//            ILogger<ContractManagementService> logger)
//        {
//            _unitOfWork = unitOfWork;
//            _fileService = fileService;
//            _environment = environment;
//            _logger = logger;
//        }

//        public async Task<ContractGenerationResult> GenerateContractAsync(Deal deal, ContractType contractType, string? reason = null)
//        {
//            var result = new ContractGenerationResult();

//            try
//            {
//                // التحقق من صحة البيانات
//                var validation = await ValidateContractAsync(deal);
//                if (!validation.IsValid)
//                {
//                    result.Success = false;
//                    result.ErrorMessage = validation.ErrorMessage;
//                    return result;
//                }

//                // أرشفة العقد السابق إذا وجد
//                if (!string.IsNullOrEmpty(deal.ContractDocumentUrl))
//                {
//                    await ArchiveOldContractAsync(deal);
//                }

//                // زيادة رقم الإصدار
//                deal.ContractVersion += 1;
//                deal.LastContractGeneratedAt = DateTime.UtcNow;

//                // إنشاء العقد الجديد
//                var contractPath = await GenerateContractPdf(deal, contractType, reason);

//                deal.ContractDocumentUrl = contractPath;
//                deal.ContractHash = await GetContractHashAsync(deal);

//                // حفظ سجل العقد
//                await SaveContractHistory(deal, contractType, reason);

//                // إرسال إشعارات للأطراف
//                await NotifyPartiesAboutNewContractAsync(deal, contractType);

//                result.Success = true;
//                result.ContractUrl = contractPath;
//                result.ContractVersion = deal.ContractVersion;
//                result.ContractHash = deal.ContractHash;

//                _logger.LogInformation($"Contract generated successfully for deal {deal.Id}, version {deal.ContractVersion}");
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, $"Error generating contract for deal {deal.Id}");
//                result.Success = false;
//                result.ErrorMessage = "Failed to generate contract";
//            }

//            return result;
//        }

//        public async Task<ContractValidationResult> ValidateContractAsync(Deal deal)
//        {
//            var result = new ContractValidationResult { IsValid = true };

//            try
//            {
//                // التحقق من وجود الأطراف
//                var owner = await _unitOfWork.Auth.GetUserById(deal.AuthorId);
//                var investor = await _unitOfWork.Auth.GetUserById(deal.InvestorId!);

//                if (owner == null)
//                {
//                    result.IsValid = false;
//                    result.ErrorMessage = "Business owner not found";
//                    return result;
//                }

//                if (investor == null)
//                {
//                    result.IsValid = false;
//                    result.ErrorMessage = "Investor not found";
//                    return result;
//                }

//                // التحقق من البيانات المطلوبة
//                if (string.IsNullOrEmpty(deal.BusinessName))
//                {
//                    result.IsValid = false;
//                    result.ErrorMessage = "Business name is required";
//                    return result;
//                }

//                if (!deal.InvestmentAmount.HasValue || deal.InvestmentAmount <= 0)
//                {
//                    result.IsValid = false;
//                    result.ErrorMessage = "Valid investment amount is required";
//                    return result;
//                }

//                if (!deal.EquityPercentage.HasValue || deal.EquityPercentage <= 0)
//                {
//                    result.IsValid = false;
//                    result.ErrorMessage = "Valid equity percentage is required";
//                    return result;
//                }

//                // التحقق من التواقيع (للعقود المحدثة)
//                if (deal.ContractVersion > 1)
//                {
//                    if (string.IsNullOrEmpty(owner.SignatureImageUrl) || !owner.IsSignatureVerified)
//                    {
//                        result.IsValid = false;
//                        result.ErrorMessage = "Business owner signature is required and must be verified";
//                        return result;
//                    }

//                    if (string.IsNullOrEmpty(investor.SignatureImageUrl) || !investor.IsSignatureVerified)
//                    {
//                        result.IsValid = false;
//                        result.ErrorMessage = "Investor signature is required and must be verified";
//                        return result;
//                    }
//                }

//                result.IsValid = true;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, $"Error validating contract for deal {deal.Id}");
//                result.IsValid = false;
//                result.ErrorMessage = "Validation error occurred";
//            }

//            return result;
//        }

//        public async Task<List<ContractHistoryItem>> GetContractHistoryAsync(int dealId)
//        {
//            var history = new List<ContractHistoryItem>();

//            try
//            {
//                var deal = await _unitOfWork.Deal.GetDealWithDetails(dealId);
//                if (deal == null) return history;

//                // إضافة العقد الحالي
//                if (!string.IsNullOrEmpty(deal.ContractDocumentUrl))
//                {
//                    history.Add(new ContractHistoryItem
//                    {
//                        Version = deal.ContractVersion,
//                        ContractUrl = deal.ContractDocumentUrl,
//                        GeneratedAt = deal.LastContractGeneratedAt ?? DateTime.UtcNow,
//                        ContractType = DetermineContractType(deal.ContractVersion),
//                        IsActive = true,
//                        Hash = deal.ContractHash
//                    });
//                }

//                // إضافة العقد السابق إذا وجد
//                if (!string.IsNullOrEmpty(deal.PreviousContractDocumentUrl))
//                {
//                    history.Add(new ContractHistoryItem
//                    {
//                        Version = deal.ContractVersion - 1,
//                        ContractUrl = deal.PreviousContractDocumentUrl,
//                        GeneratedAt = DateTime.UtcNow.AddDays(-1), // تقدير
//                        ContractType = DetermineContractType(deal.ContractVersion - 1),
//                        IsActive = false
//                    });
//                }

//                // يمكن توسيع هذا للبحث في ملفات أرشيف إضافية
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, $"Error getting contract history for deal {dealId}");
//            }

//            return history.OrderByDescending(h => h.Version).ToList();
//        }

//        public async Task<byte[]> GetContractPdfAsync(string contractUrl)
//        {
//            try
//            {
//                var fullPath = Path.Combine(_environment.WebRootPath, contractUrl.TrimStart('/'));

//                if (!File.Exists(fullPath))
//                {
//                    throw new FileNotFoundException($"Contract file not found: {contractUrl}");
//                }

//                return await File.ReadAllBytesAsync(fullPath);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, $"Error reading contract PDF: {contractUrl}");
//                throw;
//            }
//        }

//        public async Task<string> GetContractHashAsync(Deal deal)
//        {
//            try
//            {
//                var owner = await _unitOfWork.Auth.GetUserById(deal.AuthorId);
//                var investor = await _unitOfWork.Auth.GetUserById(deal.InvestorId!);

//                var contractData = $"{deal.Id}|{owner!.Id}|{investor!.Id}|{deal.InvestmentAmount}|" +
//                                   $"{deal.EquityPercentage}|{deal.DurationInMonths}|{deal.IsAutoRenew}|" +
//                                   $"{deal.ManufacturingCost}|{deal.ContractVersion}|{DateTime.UtcNow.Date}";

//                using var sha256 = SHA256.Create();
//                var bytes = Encoding.UTF8.GetBytes(contractData);
//                var hash = sha256.ComputeHash(bytes);
//                return Convert.ToHexString(hash);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, $"Error calculating contract hash for deal {deal.Id}");
//                throw;
//            }
//        }

//        public async Task<bool> ArchiveOldContractAsync(Deal deal)
//        {
//            try
//            {
//                if (string.IsNullOrEmpty(deal.ContractDocumentUrl))
//                    return true;

//                // نسخ العقد الحالي إلى مجلد الأرشيف
//                var currentPath = Path.Combine(_environment.WebRootPath, deal.ContractDocumentUrl.TrimStart('/'));
//                var archivePath = Path.Combine(_environment.WebRootPath, "Contracts", "Archive");

//                _fileService.EnsureDirectory(archivePath);

//                var fileName = Path.GetFileName(currentPath);
//                var archivedFileName = $"archived_v{deal.ContractVersion}_{fileName}";
//                var archivedPath = Path.Combine(archivePath, archivedFileName);

//                if (File.Exists(currentPath))
//                {
//                    File.Copy(currentPath, archivedPath, true);
//                    deal.PreviousContractDocumentUrl = $"/Contracts/Archive/{archivedFileName}";
//                }

//                return true;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, $"Error archiving contract for deal {deal.Id}");
//                return false;
//            }
//        }

//        public async Task<ContractComparisonResult> CompareContractVersionsAsync(int dealId, int version1, int version2)
//        {
//            var result = new ContractComparisonResult();

//            try
//            {
//                var deal = await _unitOfWork.Deal.GetDealWithDetails(dealId);
//                if (deal == null)
//                {
//                    result.Success = false;
//                    result.ErrorMessage = "Deal not found";
//                    return result;
//                }

//                // هذه دالة مبسطة - يمكن توسيعها لمقارنة حقيقية للنصوص
//                result.Differences = new List<string>
//                {
//                    $"Contract version changed from {version1} to {version2}",
//                    $"Last updated: {deal.LastContractGeneratedAt:yyyy-MM-dd HH:mm}",
//                    // يمكن إضافة المزيد من التفاصيل هنا
//                };

//                result.Success = true;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, $"Error comparing contract versions for deal {dealId}");
//                result.Success = false;
//                result.ErrorMessage = "Comparison failed";
//            }

//            return result;
//        }

//        public async Task NotifyPartiesAboutNewContractAsync(Deal deal, ContractType contractType)
//        {
//            try
//            {
//                string contractTypeText = contractType switch
//                {
//                    ContractType.Original => "original",
//                    ContractType.Renewal => "renewal",
//                    ContractType.Amendment => "amendment",
//                    _ => "updated"
//                };

//                var investorMessage = new InvestmentMessage
//                {
//                    DealId = deal.Id,
//                    SenderId = "admin", // أو معرف النظام
//                    RecipientId = deal.InvestorId!,
//                    MessageText = $"A new {contractTypeText} contract (version {deal.ContractVersion}) has been generated for deal '{deal.BusinessName}'. Please review the updated terms and conditions.",
//                    IsRead = false,
//                    MessageType = MessageType.Notification
//                };

//                var ownerMessage = new InvestmentMessage
//                {
//                    DealId = deal.Id,
//                    SenderId = "admin", // أو معرف النظام
//                    RecipientId = deal.AuthorId,
//                    MessageText = $"A new {contractTypeText} contract (version {deal.ContractVersion}) has been generated for deal '{deal.BusinessName}'. Please review the updated terms and conditions.",
//                    IsRead = false,
//                    MessageType = MessageType.Notification
//                };

//                await _unitOfWork.InvestmentMessage.AddAsync(investorMessage);
//                await _unitOfWork.InvestmentMessage.AddAsync(ownerMessage);
//                await _unitOfWork.Complete();
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, $"Error sending contract notifications for deal {deal.Id}");
//            }
//        }

//        private async Task<string> GenerateContractPdf(Deal deal, ContractType contractType, string? reason)
//        {
//            var directoryPath = Path.Combine(_environment.WebRootPath, "Contracts");
//            _fileService.EnsureDirectory(directoryPath);

//            var contractTypeText = contractType.ToString().ToLower();
//            var fileName = $"contract_deal_{deal.Id}_v{deal.ContractVersion}_{contractTypeText}_{DateTime.UtcNow.Ticks}.pdf";
//            var filePath = Path.Combine(directoryPath, fileName);

//            var owner = await _unitOfWork.Auth.GetUserById(deal.AuthorId);
//            var investor = await _unitOfWork.Auth.GetUserById(deal.InvestorId!);

//            using (var document = new Document())
//            {
//                PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(filePath, FileMode.Create));
//                document.Open();

//                var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16);
//                var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
//                var smallFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

//                // عنوان العقد
//                string contractTitle = contractType switch
//                {
//                    ContractType.Renewal => "INVESTMENT AGREEMENT - RENEWAL",
//                    ContractType.Amendment => "INVESTMENT AGREEMENT - AMENDMENT",
//                    _ => "INVESTMENT AGREEMENT"
//                };

//                document.Add(new Paragraph(contractTitle, titleFont) { Alignment = Element.ALIGN_CENTER });
//                document.Add(new Paragraph($"Contract ID: {deal.Id} (Version {deal.ContractVersion})", smallFont) { Alignment = Element.ALIGN_RIGHT });
//                document.Add(new Paragraph($"Contract Type: {contractType}", smallFont) { Alignment = Element.ALIGN_RIGHT });
//                document.Add(new Paragraph($"Date: {DateTime.UtcNow:MMMM dd, yyyy}", smallFont) { Alignment = Element.ALIGN_RIGHT });

//                if (deal.ContractVersion > 1)
//                {
//                    document.Add(new Paragraph($"Previous Version: {deal.ContractVersion - 1}", smallFont) { Alignment = Element.ALIGN_RIGHT });
//                    document.Add(new Paragraph($"Supersedes all previous agreements", smallFont) { Alignment = Element.ALIGN_RIGHT });
//                }

//                if (!string.IsNullOrEmpty(reason))
//                {
//                    document.Add(new Paragraph($"Reason for {contractType}: {reason}", smallFont) { Alignment = Element.ALIGN_RIGHT });
//                }

//                document.Add(Chunk.NEWLINE);

//                // باقي محتوى العقد...
//                document.Add(new Paragraph($"Business: {deal.BusinessName}", normalFont));
//                document.Add(new Paragraph($"Investment Amount: {deal.InvestmentAmount:C}", normalFont));
//                document.Add(new Paragraph($"Equity Percentage: {deal.EquityPercentage}%", normalFont));
//                document.Add(new Paragraph($"Duration: {deal.DurationInMonths} months", normalFont));

//                // إضافة معلومات خاصة حسب نوع العقد
//                if (contractType == ContractType.Renewal)
//                {
//                    document.Add(Chunk.NEWLINE);
//                    document.Add(new Paragraph("RENEWAL TERMS:", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12)));
//                    document.Add(new Paragraph($"This agreement renews the previous investment for {deal.DurationInMonths} months.", normalFont));
//                }
//                else if (contractType == ContractType.Amendment)
//                {
//                    document.Add(Chunk.NEWLINE);
//                    document.Add(new Paragraph("AMENDMENT DETAILS:", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12)));
//                    document.Add(new Paragraph("This amendment modifies specific terms of the original agreement.", normalFont));

//                    if (deal.ChangeAmountDifference.HasValue && Math.Abs(deal.ChangeAmountDifference.Value) > 0.01m)
//                    {
//                        if (deal.ChangeAmountDifference > 0)
//                        {
//                            document.Add(new Paragraph($"Additional investment: {deal.ChangeAmountDifference:C}", normalFont));
//                        }
//                        else
//                        {
//                            document.Add(new Paragraph($"Refund processed: {Math.Abs(deal.ChangeAmountDifference.Value):C}", normalFont));
//                        }
//                    }
//                }

//                // التوقيعات
//                document.Add(Chunk.NEWLINE);
//                document.Add(new Paragraph("ELECTRONIC SIGNATURES:", FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12)));
//                document.Add(new Paragraph($"Business Owner: {owner!.FirstName} {owner.LastName} - {DateTime.UtcNow:MM/dd/yyyy}", normalFont));
//                document.Add(new Paragraph($"Investor: {investor!.FirstName} {investor.LastName} - {DateTime.UtcNow:MM/dd/yyyy}", normalFont));

//                // معلومات التحقق
//                document.Add(Chunk.NEWLINE);
//                var documentHash = await GetContractHashAsync(deal);
//                document.Add(new Paragraph($"Document Hash: {documentHash}", smallFont) { Alignment = Element.ALIGN_CENTER });
//                document.Add(new Paragraph($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC", smallFont) { Alignment = Element.ALIGN_CENTER });

//                document.Close();
//            }

//            return $"/Contracts/{fileName}";
//        }

//        private async Task SaveContractHistory(Deal deal, ContractType contractType, string? reason)
//        {
//            // يمكن إضافة جدول منفصل لتاريخ العقود إذا لزم الأمر
//            // حالياً نحفظ المعلومات في Deal نفسه
//        }

//        private ContractType DetermineContractType(int version)
//        {
//            return version switch
//            {
//                1 => ContractType.Original,
//                _ => ContractType.Amendment // افتراض أن الإصدارات اللاحقة هي تعديلات
//            };
//        }
//    }

//    // Enums و Classes مساعدة
//    public enum ContractType
//    {
//        Original,
//        Renewal,
//        Amendment
//    }

//    public class ContractGenerationResult
//    {
//        public bool Success { get; set; }
//        public string? ErrorMessage { get; set; }
//        public string? ContractUrl { get; set; }
//        public int ContractVersion { get; set; }
//        public string? ContractHash { get; set; }
//    }

//    public class ContractValidationResult
//    {
//        public bool IsValid { get; set; }
//        public string? ErrorMessage { get; set; }
//        public List<string> Warnings { get; set; } = new();
//    }

//    public class ContractHistoryItem
//    {
//        public int Version { get; set; }
//        public string ContractUrl { get; set; } = string.Empty;
//        public DateTime GeneratedAt { get; set; }
//        public ContractType ContractType { get; set; }
//        public bool IsActive { get; set; }
//        public string? Hash { get; set; }
//    }

//    public class ContractComparisonResult
//    {
//        public bool Success { get; set; }
//        public string? ErrorMessage { get; set; }
//        public List<string> Differences { get; set; } = new();
//    }
//}
