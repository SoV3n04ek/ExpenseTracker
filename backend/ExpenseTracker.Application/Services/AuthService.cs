using ExpenseTracker.Application.Configuration;
using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Application.Interfaces;
using ExpenseTracker.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration; // to read JWT secret
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;     // for security key
using System.IdentityModel.Tokens.Jwt;    // for JwtSecurityToken
using System.Security.Claims;
using System.Text;

namespace ExpenseTracker.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly JwtSettings _jwtSettings;
        private readonly IConfiguration _configuration;

        public AuthService(
            UserManager<ApplicationUser> userManager, 
            IOptions<JwtSettings> jwtOptions, 
            IEmailService emailService,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _jwtSettings = jwtOptions.Value;
            _emailService = emailService;
            _configuration = configuration;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);

            if (existingUser != null)
            {
                if (!existingUser.EmailConfirmed)
                {
                    await SendConfirmationEmail(existingUser);
                    return new AuthResponseDto
                    {
                        Email = dto.Email,
                        Message = "User already exists but email is unconfirmed. A new confirmation email has been sent."
                    };
                }

                return new AuthResponseDto
                {
                    Email = dto.Email,
                    Errors = new[] { "Registration failed: Email is already in use" }
                };
            }

            // normal registration flow
            var user = new ApplicationUser 
            { 
                UserName = dto.Email, 
                Email = dto.Email,
                Name = dto.Name
            };
            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                return new AuthResponseDto
                {
                    Email = dto.Email,
                    Errors = result.Errors.Select(e => e.Description)
                };
            }

            await SendConfirmationEmail(user);

            return new AuthResponseDto { 
                Email = user.Email,
                Name = user.Name,
                Message = "Please check your email to confirm your account." };
        }

        private async Task SendConfirmationEmail(ApplicationUser user)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            
            var encodedToken = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(
                System.Text.Encoding.UTF8.GetBytes(token));

            // Pointing to Frontend UI (ConfirmEmailComponent)
            var baseUrl = _configuration["ClientSettings:BaseUrl"] ?? "http://localhost:4200";
            var confirmationLink = $"{baseUrl.TrimEnd('/')}/confirm-email?userId={user.Id}&token={encodedToken}";

            await _emailService.SendEmailAsync(user.Email!, "Confirm your email",
                $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.");

        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            {
                throw new UnauthorizedAccessException("Invalid email or password");
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                throw new UnauthorizedAccessException("User is locked out");
            }

            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                throw new UnauthorizedAccessException("You must confirm your email before logging in.");
            }

            return await GenerateAuthResponse(user);
        }

        public async Task<AuthResponseDto> GetCurrentUserAsync(int userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                throw new UnauthorizedAccessException("User not found");
            }

            return await GenerateAuthResponse(user);
        }

        private Task<AuthResponseDto> GenerateAuthResponse(ApplicationUser user)
        {
            // 1. Define the claims (user data we want in the token)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Name, user.Name)
            };

            // 2. Get the secret key from configuration
            var jwtKey = _jwtSettings.Key ??
                throw new InvalidOperationException("Jwt key is not configured.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

            // 3. Set expiration and signing credentials
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiration = DateTime.Now.AddMinutes(Convert.ToDouble(_jwtSettings.ExpireMinutes));

            // 4. Create the token structure
            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expiration,
                signingCredentials: credentials
            );

            // 5. Serialize and return the Auth Response DTO
            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            var responseDto = new AuthResponseDto
            {
                Token = tokenString,
                Expiration = expiration,
                UserId = user.Id.ToString(),
                Email = user.Email,
                Name = user.Name
            };

            return Task.FromResult(responseDto);
        }

        public async Task<bool> ConfirmEmailAsync(int userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null) return false;

            // handles the actual verification logic
            var result = await _userManager.ConfirmEmailAsync(user, token);
            return result.Succeeded;
        }

        public async Task ForgotPasswordAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            
            // Security Rule: If the user is null, return immediately to prevent email enumeration.
            // We return a "success" (Task.CompletedTask) to keep the API response identical.
            if (user == null) return;

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            
            var encodedToken = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(
                System.Text.Encoding.UTF8.GetBytes(token));
            var encodedEmail = System.Net.WebUtility.UrlEncode(email);

            // {BaseUrl}/reset-password?token={token}&email={email}
            var baseUrl = _configuration.GetValue<string>("ClientSettings:BaseUrl") ?? "http://localhost:4200";
            var resetLink = $"{baseUrl.TrimEnd('/')}/reset-password?token={encodedToken}&email={encodedEmail}";
            Console.WriteLine($"\n\n[CONFIG CHECK] Frontend BaseUrl is: {baseUrl}\n\n");
            await _emailService.SendEmailAsync(email, "Reset your password",
                $"Please reset your password by <a href='{resetLink}'>clicking here</a>.");
        }

        public async Task<IdentityResult> ResetPasswordAsync(ResetPasswordRequest dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });
            }

            var decodedToken = System.Text.Encoding.UTF8.GetString(
                Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(dto.Token));

            return await _userManager.ResetPasswordAsync(user, decodedToken, dto.NewPassword);
        }
    }
}
