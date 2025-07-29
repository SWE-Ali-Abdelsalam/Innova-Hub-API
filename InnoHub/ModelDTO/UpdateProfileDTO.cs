using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class UpdateProfileDTO
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50, ErrorMessage = "First name can't be longer than 50 characters.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(50, ErrorMessage = "Last name can't be longer than 50 characters.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "City is required.")]
        [StringLength(100, ErrorMessage = "City name can't be longer than 100 characters.")]
        public string City { get; set; }

        [Required(ErrorMessage = "District is required.")]
        [StringLength(100, ErrorMessage = "District name can't be longer than 100 characters.")]
        public string District { get; set; }
        [Required(ErrorMessage = "Country is required.")]
        [StringLength(100, ErrorMessage = "Country name can't be longer than 100 characters.")]
        public string Country { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Invalid phone number format.")]
        public string PhoneNumber { get; set; }
    }
}
