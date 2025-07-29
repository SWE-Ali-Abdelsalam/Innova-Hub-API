using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class UpdatePasswordDTO
    {
        [Required(ErrorMessage = "Current password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Current password must be between 6 and 100 characters.")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "New password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "New password must be between 6 and 100 characters.")]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[!@#$%^&*]).{6,}$",
            ErrorMessage = "New password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character.")]
        public string NewPassword { get; set; }
    }
}
