using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Application.Interfaces;
using ExpenseTracker.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseTracker.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IEmailService _emailService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AuthController(
            IAuthService authService,
            IEmailService emailService,
            UserManager<ApplicationUser> userManager)
        {
            _authService = authService;
            _emailService = emailService;
            _userManager = userManager;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto dto)
        {
            var response = await _authService.RegisterAsync(dto);
            return Ok(response);
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
        {
            var response = await _authService.LoginAsync(dto);
            return Ok(response);
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(int userId, string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { message = "Token is required" });
            }

            try
            {
                var decodedTokenBytes = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(token);
                var decodedToken = System.Text.Encoding.UTF8.GetString(decodedTokenBytes);
                
                var result = await _authService.ConfirmEmailAsync(userId, decodedToken);

                if (result)
                {
                    return Ok(new { message = "Email confirmed successfully! You can now log in." });
                }

                return BadRequest(new { message = "Email confirmation failed. The token might be invalid or expired." });
            }
            catch (FormatException)
            {
                return BadRequest(new { message = "Invalid token format." });
            }
        }

        [HttpPost("resend-confirmation")]
        public async Task<IActionResult> ResendConfirmation([FromBody] ResendEmailRequest dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user is null || await _userManager.IsEmailConfirmedAsync(user))
            {
                return Ok(new { message = "If the account exists and is unconfirmed, an email has been sent." });
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            var encodedToken = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(
                System.Text.Encoding.UTF8.GetBytes(token));

            var confirmationLink = $"http://localhost:4200/confirm-email?userId={user.Id}&token={encodedToken}";

            await _emailService.SendEmailAsync(user.Email!, "Confirm your email", 
                $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.");

            return Ok(new { message = "Confirmation email resent." });
        }
    }
}
