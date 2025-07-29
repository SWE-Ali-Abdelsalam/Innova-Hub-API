using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class ChangeEmailDTO
    {
        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "New email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string NewEmail { get; set; }
    }
}
