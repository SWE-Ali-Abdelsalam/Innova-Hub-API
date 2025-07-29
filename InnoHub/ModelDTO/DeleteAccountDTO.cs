using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class DeleteAccountDTO
    {
        public string? UserId { get; set; }

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters.")]
        public string? Password { get; set; }
    }
}
