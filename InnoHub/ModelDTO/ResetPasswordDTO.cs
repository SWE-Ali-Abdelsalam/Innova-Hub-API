using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class ResetPasswordDTO
    {
       
        [Required(ErrorMessage = "New password is required.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters.")]
        public string NewPassword { get; set; }
        public string Token { get; set; }
    }
}
