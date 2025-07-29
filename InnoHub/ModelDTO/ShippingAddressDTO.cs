using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class ShippingAddressDTO
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Street address is required.")]
        [StringLength(100, ErrorMessage = "Street address cannot exceed 100 characters.")]
        public string StreetAddress { get; set; }

        [StringLength(50, ErrorMessage = "Apartment name cannot exceed 50 characters.")]
        public string Apartment { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Invalid phone number format.")]
        [StringLength(15, MinimumLength = 10, ErrorMessage = "Phone number should be between 10 to 15 characters.")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "City is required.")]
        [StringLength(50, ErrorMessage = "City name cannot exceed 50 characters.")]
        public string City { get; set; }

        [Required(ErrorMessage = "Zip code is required.")]
        [RegularExpression(@"^\d{5,10}$", ErrorMessage = "Invalid Zip code format.")]
        public string ZipCode { get; set; }
    }
}
