using Microsoft.AspNetCore.Identity;
using System;

namespace InnoHub.Core.Models
{
    public class AppUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? City { get; set; }
        public string? District { get; set; }
        public string? Country { get; set; }
        public string ProfileImageUrl { get; set; } = "/ProfileImages/DefaultImage.png";
        public string ProfileCoverUrl { get; set; } = "/ProfileImages/DefaultCover.jpg";
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public decimal TotalAccountBalance { get; set; }

        // Add this property to indicate if the user logged in via an external provider (Google, Facebook, etc.)
        public bool IsExternalLogin { get; set; }
        public Deal? Deal { get; set; }
        public ICollection<Product> Products { get; set; }
        public ICollection<ProductRating> Ratings { get; set; }
        public string? StripeAccountId { get; set; }
        public bool IsStripeAccountEnabled { get; set; }

        // ID verification fields
        public string? IdCardFrontImageUrl { get; set; }
        public string? IdCardBackImageUrl { get; set; }
        public bool IsIdCardVerified { get; set; } = false;
        public DateTime? IdCardUploadDate { get; set; }
        public DateTime? IdCardVerificationDate { get; set; }
        public string? IdCardVerifiedByUserId { get; set; } // AdminId who verified
        public string? IdCardRejectionReason { get; set; }

        // Signature related properties
        public string? SignatureImageUrl { get; set; }
        public DateTime? SignatureUploadDate { get; set; }
        public bool IsSignatureVerified { get; set; } = false;
        public DateTime? SignatureVerificationDate { get; set; }
        public string? SignatureVerifiedByUserId { get; set; } // AdminId who verified
        public string? SignatureRejectionReason { get; set; }
        public bool Isblock { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginedAt { get; set; }

        //==========================================================

        public string? Segment { get; set; }
        public string? Region { get; set; }
        public string PostalCode { get; set; } = "11511";

        //==========================================================

        public string? MobileFcmToken { get; set; }      // للفلاتر
        public string? WebFcmToken { get; set; }         // للريأكت
        public DateTime? FcmTokenUpdatedAt { get; set; }
        public string? LastActivePlatform { get; set; }   // "mobile" | "web"
    }
}
