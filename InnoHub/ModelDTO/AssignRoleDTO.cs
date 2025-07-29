using System.ComponentModel.DataAnnotations;

namespace InnoHub.ModelDTO
{
    public class AssignRoleDTO
    {
        [Required(ErrorMessage = "UserId is required.")]
        
        public string UserId { get; set; }

        [Required(ErrorMessage = "RoleName is required.")]
       
        public string RoleId { get; set; }
    }
}
