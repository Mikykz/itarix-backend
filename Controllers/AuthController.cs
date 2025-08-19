using Itarix.Api.Business;
using Itarix.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;

namespace Itarix.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly string _jwtKey;
        private readonly string _jwtIssuer;
        private readonly string _jwtAudience;
        private readonly IEmailSender _emailSender;
        private readonly string _baseUrl;

        public AuthController(UserService userService, IConfiguration config, IEmailSender emailSender)
        {
            _userService = userService;
            _jwtKey = config["Jwt:Key"] ?? throw new ArgumentNullException(nameof(_jwtKey), "JWT key is missing from configuration.");
            _jwtIssuer = config["Jwt:Issuer"] ?? "ItarixAPI";
            _jwtAudience = config["Jwt:Audience"] ?? "ItarixClients";
            _emailSender = emailSender;
            _baseUrl = config["App:BaseUrl"] ?? "https://yourapp.com";
        }

        /// <summary>
        /// Registers a new user and sends email verification link.
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                var userId = _userService.RegisterUser(dto);
                var user = _userService.GetUserById(userId);

                var person = _userService.GetPersonById(user.PersonId);
                var verifyUrl = $"{_baseUrl}/verify-email?token={user.EmailVerificationToken}";

                await _emailSender.SendEmailAsync(person.Email, "Verify your email address",
                    $"<p>Welcome! Click <a href='{verifyUrl}'>here</a> to verify your email address.</p>");

                return Ok(new { message = "Registration successful. Please verify your email before logging in." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Registration failed: {ex.Message}" });
            }
        }

        /// <summary>
        /// Verifies the user's email using the provided token.
        /// </summary>
        [HttpGet("verify-email")]
        public IActionResult VerifyEmail([FromQuery] string token)
        {
            var user = _userService.GetUserByEmailToken(token);
            if (user == null)
                return BadRequest(new { error = "Invalid or expired token." });

            user.IsEmailConfirmed = true;
            user.EmailVerificationToken = null;
            _userService.UpdateUser(user);

            return Ok(new { message = "Email verified! You can now log in." });
        }

        /// <summary>
        /// Logs in the user and issues access and refresh tokens.
        /// </summary>
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto dto)
        {
            var user = _userService.Login(dto.Username, dto.Password);
            if (user == null)
                return Unauthorized(new { error = "Invalid username or password." });

            if (!user.IsEmailConfirmed)
                return Unauthorized(new { error = "Please verify your email before logging in." });

            var tokens = GenerateTokens(user);

            return Ok(new
            {
                userId = user.UserId,
                username = user.Username,
                role = user.Role,
                accessToken = tokens.token,
                refreshToken = tokens.refreshToken
            });
        }

        /// <summary>
        /// Refreshes the JWT access token using a valid refresh token.
        /// </summary>
        [HttpPost("refresh-token")]
        public IActionResult RefreshToken([FromBody] RefreshTokenDto dto)
        {
            var user = _userService.GetUserByRefreshToken(dto.RefreshToken);
            if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
                return Unauthorized(new { error = "Invalid or expired refresh token." });

            var tokens = GenerateTokens(user);

            return Ok(new
            {
                accessToken = tokens.token,
                refreshToken = tokens.refreshToken
            });
        }

        /// <summary>
        /// Initiates password reset process via email.
        /// </summary>
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] EmailRequestDto dto)
        {
            try
            {
                var token = _userService.ForgotPassword(dto.Email);
                if (!string.IsNullOrEmpty(token))
                {
                    var resetUrl = $"{_baseUrl}/reset-password?token={token}";
                    await _emailSender.SendEmailAsync(dto.Email, "Reset your password",
                        $"<p>Click <a href='{resetUrl}'>here</a> to reset your password. This link will expire in 15 minutes.</p>");
                }
            }
            catch
            {
                // Intentionally silent to prevent email enumeration
            }

            return Ok(new { message = "If this email is registered, you will receive reset instructions shortly." });
        }

        /// <summary>
        /// Completes the password reset process using the reset token.
        /// </summary>
        [HttpPost("reset-password")]
        public IActionResult ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var user = _userService.GetUserByPasswordResetToken(dto.Token);
            if (user == null || user.PasswordResetExpiry < DateTime.UtcNow)
                return BadRequest(new { error = "Invalid or expired token." });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetExpiry = null;
            _userService.UpdateUser(user);

            return Ok(new { message = "Password has been reset successfully." });
        }

        /// <summary>
        /// Logs out the user by invalidating their refresh token.
        /// </summary>
        [HttpPost("logout")]
        public IActionResult Logout([FromBody] string refreshToken)
        {
            var user = _userService.GetUserByRefreshToken(refreshToken);
            if (user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiry = null;
                _userService.UpdateUser(user);
            }
            return Ok(new { message = "Logged out successfully." });
        }

        /// <summary>
        /// Retrieves the current authenticated user's ID (test endpoint).
        /// </summary>
        [Authorize]
        [HttpGet("test-userid")]
        public IActionResult TestUserId()
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value;
            if (string.IsNullOrEmpty(userIdStr))
                return Unauthorized(new { error = "UserId claim missing." });

            return Ok(new { UserId = userIdStr });
        }

        /// <summary>
        /// Shows all claims for debugging (Admin only).
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("debug-claims")]
        public IActionResult DebugClaims()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value });
            return Ok(claims);
        }

        [Authorize]
        [HttpGet("validate")]
        public IActionResult Validate()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? User.FindFirst("userId")?.Value; // fallback to custom

            var username = User.FindFirstValue(ClaimTypes.Name)
                         ?? User.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value;

            if (string.IsNullOrEmpty(userIdStr))
                return Unauthorized(new { error = "Invalid token." });

            return Ok(new
            {
                message = "Token is valid",
                userId = userIdStr,
                username = username
            });
        }



        /// <summary>
        /// Generates a new JWT access token and refresh token for a given user.
        /// </summary>
        // wherever you generate tokens (e.g., AuthService/AuthController)
        private (string token, string refreshToken) GenerateTokens(User user)
        {
            // Pull email (and optionally name) from Person
            var person = _userService.GetPersonById(user.PersonId);
            var email = person?.Email ?? string.Empty;
            var name = string.IsNullOrWhiteSpace(user.Username) ? (person?.Name ?? "") : user.Username;
            var role = string.IsNullOrWhiteSpace(user.Role) ? "user" : user.Role;

            var token = JwtTokenHelper.GenerateToken(
                user.UserId,   // user id
                name,          // username/display name
                email,         // <-- from Person
                role,
                _jwtKey,
                _jwtIssuer,
                _jwtAudience,
                15             // 15 min
            );

            var refreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            _userService.UpdateUser(user);

            return (token, refreshToken);
        }



    }
}
