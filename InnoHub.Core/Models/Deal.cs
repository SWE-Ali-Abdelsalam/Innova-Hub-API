using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InnoHub.Core.Models
{
    public class Deal 
    {
        public int Id { get; set; }

        [Required]
        public string? AuthorId { get; set; }
        public AppUser? Author { get; set; }
        public string BusinessName { get; set; }

        public string Description { get; set; }

        public decimal OfferMoney { get; set; } // Use decimal for monetary values

        public decimal OfferDeal { get; set; } // Use decimal for precision

        public List<string> Pictures { get; set; } = new List<string>();

        // Nullable Foreign Key for "On Delete Set Null"
        public int CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public Category? Category { get; set; }
        public bool IsApproved { get; set; } = false; // Marks if the product is approved by the admin
        public bool IsVisible { get; set; } = true;
        public DateTime? ApprovedAt { get; set; } // Stores the date the product was approved
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public decimal ManufacturingCost { get; set; }
        public decimal EstimatedPrice { get; set; }

        // ========== Investment ==========

        public string? InvestorId { get; set; }
        public AppUser? Investor { get; set; }
        public DealStatus? Status { get; set; }

        //public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AcceptedByOwnerAt { get; set; }
        public DateTime? ApprovedByAdminAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public decimal PlatformFeePercentage { get; set; } = 1;
        public ICollection<DealMessage> Messages { get; set; } = new List<DealMessage>();
        public ICollection<DealProfit> ProfitDistributions { get; set; } = new List<DealProfit>();
        public int? ProductId { get; set; }  // Link to the product created from this deal
        public Product? Product { get; set; }  // Navigation property

        public bool IsProductCreated { get; set; } = false;  // Flag to track if product has been created
        public bool IsReadyForProduct { get; set; } = false;  // Flag to indicate deal is eligible for product creation

        public int DurationInMonths { get; set; }
        public DateTime? ScheduledEndDate { get; set; } // Calculated based on CompletedAt + Duration
        public bool CompletionRequestedByOwner { get; set; } = false;
        public bool CompletionRequestedByInvestor { get; set; } = false;
        public DateTime? OwnerCompletionRequestDate { get; set; }
        public DateTime? InvestorCompletionRequestDate { get; set; }

        public DateTime? ActualEndDate { get; set; } // When deal was actually terminated
        public DealEndReason? EndReason { get; set; } // Reason for termination
        public string? TerminationNotes { get; set; } // Notes about termination
        public bool TerminationRequestedByOwner { get; set; } = false;
        public bool TerminationRequestedByInvestor { get; set; } = false;
        public DateTime? OwnerTerminationRequestDate { get; set; }
        public DateTime? InvestorTerminationRequestDate { get; set; }
        public string? TerminationRequestReason { get; set; }
        public bool IsTerminationEscalatedToAdmin { get; set; } = false;
        public DateTime? TerminationEscalationDate { get; set; }

        // Payment-related properties
        public string? PaymentIntentId { get; set; }  // Stripe Payment Intent ID for initial deal
        public string? PaymentClientSecret { get; set; }  // Client secret for mobile SDK integration
        public bool IsPaymentProcessed { get; set; } = false;
        public DateTime? PaymentProcessedAt { get; set; }
        public string? PaymentStatus { get; set; }
        public string? PaymentError { get; set; }
        public string? StripeAccountId { get; set; }  // Business owner's connected Stripe account
        public string? Platform { get; set; }  // "web" or "mobile" - tracks which platform initiated payment

        // Contract and signature fields
        public string? ContractDocumentUrl { get; set; }
        public bool IsOwnerSigned { get; set; } = false;
        public DateTime? OwnerSignedAt { get; set; }
        public bool IsInvestorSigned { get; set; } = false;
        public DateTime? InvestorSignedAt { get; set; }
        public string? ContractHash { get; set; } // SHA-256 hash of contract for verification

        // Change
        public DealChangeRequest? DealChangeRequest { get; set; }
        public DealDeleteRequest? DealDeleteRequest { get; set; }

        // ========== Change Tracking ==========
        public bool IsChangePaymentRequired { get; set; } = false;
        public string? ChangePaymentIntentId { get; set; }
        public bool IsChangePaymentProcessed { get; set; } = false;
        public DateTime? ChangePaymentProcessedAt { get; set; }
        public decimal? ChangeAmountDifference { get; set; } // موجب = المستثمر يدفع، سالب = رد أموال
        public int? ChangeRequestId { get; set; } // FK to DealChangeRequest

        // ========== Contract Versioning ==========
        public int ContractVersion { get; set; } = 1;
        public DateTime? LastContractGeneratedAt { get; set; }
        public string? PreviousContractDocumentUrl { get; set; } // للاحتفاظ بالعقد السابق

        // ========== Payment Prevention ==========
        public string? LastProcessedPaymentHash { get; set; } // لمنع تكرار الدفع
    }
}
