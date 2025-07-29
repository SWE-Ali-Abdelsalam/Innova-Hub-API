using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class RegisterUserDTO
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(50, ErrorMessage = "First name cannot be longer than 50 characters.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(50, ErrorMessage = "Last name cannot be longer than 50 characters.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm password is required.")]
        [Compare("Password", ErrorMessage = "Password and Confirm Password do not match.")]
        public string ConfirmPassword { get; set; }

        [StringLength(100, ErrorMessage = "City name cannot be longer than 100 characters.")]
        public string City { get; set; }

        [Phone(ErrorMessage = "Invalid phone number.")]
        public string PhoneNumber { get; set; }

        [StringLength(100, ErrorMessage = "District name cannot be longer than 100 characters.")]
        public string District { get; set; }
        [StringLength(100, ErrorMessage = "Country name cannot be longer than 100 characters.")]
        public string Country { get; set; }
        [Required(ErrorMessage = "Role ID is required.")]
        [StringLength(50, ErrorMessage = "Role ID cannot be longer than 50 characters.")]
        public string RoleId { get; set; }
    }
}
