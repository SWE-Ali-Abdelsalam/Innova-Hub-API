using AutoMapper;
using Ecommerce_platforms.Repository.Auth;
using InnoHub.Core.Models;
using InnoHub.ModelDTO;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.Extensions.Logging;



namespace InnoHub.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly UserManager<AppUser> _userManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly IAuth _auth;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            RoleManager<IdentityRole> roleManager,
            UserManager<AppUser> userManager,
            IUnitOfWork unitOfWork,
            SignInManager<AppUser> signInManager,
            IAuth auth,
            IMapper mapper,
            ILogger<AccountController> logger
            )
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _unitOfWork = unitOfWork;
            _signInManager = signInManager;
            _auth = auth;
            _mapper = mapper;
            _logger = logger;
            
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO loginDTO)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(loginDTO.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, loginDTO.Password))
                return Unauthorized("Invalid email or password.");

            // Check if the user is blocked
            if (user.Isblock)
            {
                return Unauthorized("Your account is blocked. Please contact support.");
            }

            await _signInManager.SignInAsync(user, isPersistent: false);

            var roleName = await _auth.GetRoleNameAsync(user);
            var token = await _auth.CreateToken(user, _userManager);

            user.LastLoginedAt = DateTime.UtcNow;
            await _unitOfWork.AppUser.UpdateAsync(user);
            await _unitOfWork.Complete();

            return Ok(new LoginResponseDTO
            {
                Message = "User Login Successfully",
                UserId = user.Id,
                Email = loginDTO.Email,
                RoleName = roleName,
                Token = token
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterUserDTO registerDTO)
        {
            // Check if the provided Role exists before creating the user
            var role = await _roleManager.FindByIdAsync(registerDTO.RoleId); // Use RoleName instead of RoleId
            if (role == null)
            {
                return BadRequest(new { Message = $"Role '{registerDTO.RoleId}' does not exist." });
            }

            // Check if a user with the provided email already exists
            var user = await _userManager.FindByEmailAsync(registerDTO.Email);
            if (user != null)
            {
                return BadRequest(new { Message = "Email already exists" });
            }

            // Create the application user
            var applicationUser = new AppUser
            {
                Email = registerDTO.Email,
                City = registerDTO.City,
                PhoneNumber = registerDTO.PhoneNumber,
                UserName = registerDTO.Email.Split("@")[0], // Use email part before '@' as username
                FirstName = registerDTO.FirstName,
                LastName = registerDTO.LastName,
                Country = registerDTO.Country,
                District = registerDTO.District,
                Isblock = false,
                LastLoginedAt = DateTime.UtcNow
            };

            // Create the user in the database
            var result = await _userManager.CreateAsync(applicationUser, registerDTO.Password);
            if (!result.Succeeded)
            {
                return BadRequest(new { Message = "Failed to register user", Errors = result.Errors.Select(e => e.Description) });
            }

            // Assign the user to the provided role
            var roleAssignResult = await _userManager.AddToRoleAsync(applicationUser, role.Name);
            if (!roleAssignResult.Succeeded)
            {
                return BadRequest(new { Message = "Failed to assign role", Errors = roleAssignResult.Errors.Select(e => e.Description) });
            }
            
            // Generate the authentication token for the user
            var userToken = await _auth.CreateToken(applicationUser, _userManager); // Ensure CreateToken is correctly implemented

            //if(role.Name == "BusinessOwner")
            //{
            //    await _unitOfWork.Owner.AddAsync(new Owner { Id = applicationUser.Id });
            //    await _unitOfWork.Complete();
            //}

            // Return user details and token
            return Ok(new
            {
                UserId = applicationUser.Id,
                Role = role.Name,
                Email = applicationUser.Email,
                Token = userToken
            });
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetAllRoles()
        {
            var roles = await _roleManager.Roles
                .Select(r => new { r.Id, r.Name })
                .ToListAsync();

            return Ok(roles);
        }

        [HttpPost("assignToRole")]
        public async Task<IActionResult> AssignRoleToUser(
    [FromHeader(Name = "Authorization")] string authorizationHeader,
    [FromBody] AssignRoleDTO assignRoleDTO)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Get user ID from token
            var userId = _auth.GetUserIdFromToken(authorizationHeader);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "User not authenticated." });

            // Get the current user based on the userId extracted from the token
            var currentUser = await _userManager.FindByIdAsync(userId);
            if (currentUser == null)
                return Unauthorized(new { Message = "User not found." });

            // Check if the current user has the "Admin" role
            var currentUserRoles = await _userManager.GetRolesAsync(currentUser);
            if (!currentUserRoles.Contains("Admin"))
                return Unauthorized(new { Message = "Only admins can assign roles." });

            // Find the target user to assign the role to
            var user = await _userManager.FindByIdAsync(assignRoleDTO.UserId);
            if (user == null)
                return BadRequest(new { Message = "User not found." });

            // Find the role to assign to the user
            var role = await _roleManager.FindByIdAsync(assignRoleDTO.RoleId);
            if (role == null)
                return BadRequest(new { Message = "Role not found." });

            // Check if the user already has the role
            var userRoles = await _userManager.GetRolesAsync(user);
            if (userRoles.Contains(role.Name))
                return BadRequest(new { Message = "User already has this role." });

            // Assign the role to the target user
            var result = await _userManager.AddToRoleAsync(user, role.Name);
            if (!result.Succeeded)
                return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });

            return Ok(new { Message = "Role assigned successfully." });
        }

        [HttpGet("google-login")]
        public IActionResult GoogleLogin()
        {
            // Redirect URL to which Google will send the user after successful authentication
            var redirectUrl = Url.Action(nameof(GoogleResponse), "Account", null, Request.Scheme);

            // Create properties for external authentication
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(GoogleDefaults.AuthenticationScheme, redirectUrl);

            // Challenge to trigger the authentication with Google
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet("google-response")]
        public async Task<IActionResult> GoogleResponse()
        {
            // Get external login information
            var info = await _signInManager.GetExternalLoginInfoAsync();

            // Handle the case where login info could not be retrieved
            if (info == null)
            {
                return BadRequest(new { Message = "Unable to load external login information." });
            }

            // Get the user's email from the login info
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);

            // Handle the case where email is not available
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(new { Message = "Unable to retrieve email from external login." });
            }

            // Check if the user already exists in the database by email
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null)
            {
                // User exists, sign them in
                await _signInManager.SignInAsync(existingUser, isPersistent: false);

                // Retrieve user roles
                var roles = await _userManager.GetRolesAsync(existingUser);

                // Generate authentication token for the user
                var token = await _auth.CreateToken(existingUser, _userManager);

                // Set the frontend URL (localhost:5173)
                string frontendUrl = "https://innova-hub.premiumasp.net";  // This can be made configurable

                // Redirect to frontend with user information and token as query parameters
                return Redirect($"{frontendUrl}/login-success?userId={existingUser.Id}&email={existingUser.Email}&roleName={roles.FirstOrDefault() ?? "No Role"}&token={token}");
            }

            // If the user does not exist, create a new user
            var result = await _auth.CreateExternalUserAsync(info, email);
            if (result == null || !result.Success)
            {
                return BadRequest( new { Message = "An error occurred while creating the user." });
            }

            // If new user creation is successful, redirect to frontend with the user data
            string frontendUrlNew = "https://innova-hub.premiumasp.net";  // This can also be configurable
            return Redirect($"{frontendUrlNew}/login-success?userId={result.UserId}&email={result.Email}&roleName={result.RoleName}&token={result.Token}");
        }
    }
}
