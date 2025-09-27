using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ChatRoom_API.Hubs;
<<<<<<< HEAD
using ChatRoom_API.Interfecae;
=======
using ChatRoom_API.Interface;
>>>>>>> a4a5677 (Updated frontend (Angular) and backend (.NET) with new features)
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace ChatRoom_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IOnlineUserService _onlineUserService;

        public AuthController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IConfiguration configuration,
            IHubContext<ChatHub> hubContext,
            IOnlineUserService onlineUserService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _hubContext = hubContext;
            _onlineUserService = onlineUserService;
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok("✅ AuthController is working.");
        }

        // ===================== REGISTER =====================
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (await _userManager.FindByEmailAsync(model.Email) != null)
                return BadRequest(new { message = "User with this email already exists!" });

            var user = new IdentityUser
            {
                UserName = model.Username,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
                return BadRequest(new
                {
                    message = "Registration failed!",
                    errors = result.Errors.Select(e => e.Description)
                });

            return Ok(new { message = "User registered successfully!" });
        }

        // ===================== LOGIN =====================
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                return Unauthorized(new { message = "Invalid username or password!" });

            var token = GenerateJwtToken(user);

            // Add user to online list (but connectionId will be updated when SignalR connects)
            _onlineUserService.AddUser(user.UserName, "PENDING_CONNECTION_ID");

            // Notify all clients that the user is online
            await _hubContext.Clients.All.SendAsync("UserStatusChanged", user.UserName, "connected");

            return Ok(new
            {
                token,
                username = user.UserName,
                message = $"{user.UserName} is now online."
            });
        }

        // ===================== LOGOUT =====================
        [Authorize(AuthenticationSchemes =JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var username = User?.Identity?.Name;
            await _signInManager.SignOutAsync();

            if (!string.IsNullOrEmpty(username))
            {
                // Remove user from the online list
                _onlineUserService.RemoveUserByUsername(username);

                var updatedOnlineUsers = _onlineUserService.GetOnlineUsers();


                // Notify all clients about the user logging out
                await _hubContext.Clients.All.SendAsync("UserStatusChanged", username, "disconnected");
                await _hubContext.Clients.All.SendAsync("UpdateUserList", updatedOnlineUsers);

                Console.WriteLine($"❌ {username} logged out. Online users: {string.Join(", ", updatedOnlineUsers)}");



                // Optionally, you can send a message to the client
                return Ok(new { message = "User logged out successfully!" });
            }

            return Unauthorized(new { message = "Invalid request or user not authenticated!" });
        }


        // ===================== TOKEN GENERATION =====================
        private string GenerateJwtToken(IdentityUser user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = jwtSettings["Key"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];

            // Ensure required settings exist
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
            {
                throw new Exception("JWT configuration is missing. Check appsettings.json.");
            }

            var expireHours = int.Parse(jwtSettings["ExpireHours"] ?? "1");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(expireHours),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class RegisterModel
    {
        [Required]
        public string Username { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, MinLength(6)]
        public string Password { get; set; }
    }

    public class LoginModel
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
