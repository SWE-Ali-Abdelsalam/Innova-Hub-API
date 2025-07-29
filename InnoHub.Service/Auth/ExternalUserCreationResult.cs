using InnoHub.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InnoHub.Service.Auth
{
    public class ExternalUserCreationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string UserId { get; set; }
        public string Email { get; set; }
        public string Token { get; set; }
        public IEnumerable<string> Errors { get; set; }

        // Add these properties to make them accessible in the controller
        public string RoleName { get; set; }  // User's role
        public AppUser User { get; set; }  // ApplicationUser object (replace with your user type)
    }
}
