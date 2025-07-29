using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class EditUserAccountDTO
    {
        
        [StringLength(50, ErrorMessage = "First name can't be longer than 50 characters.")]
        public string FirstName { get; set; }

        [StringLength(50, ErrorMessage = "Last name can't be longer than 50 characters.")]
        public string LastName { get; set; }

        [StringLength(100, ErrorMessage = "City name can't be longer than 100 characters.")]
        public string City { get; set; }

        [StringLength(100, ErrorMessage = "District name can't be longer than 100 characters.")]
        public string District { get; set; }
        [StringLength(100, ErrorMessage = "Country name can't be longer than 100 characters.")]
        public string Country { get; set; }

        [Phone(ErrorMessage = "Invalid phone number format.")]
        public string PhoneNumber { get; set; }
    }
}
