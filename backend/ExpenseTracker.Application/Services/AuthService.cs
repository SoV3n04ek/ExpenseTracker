using ExpenseTracker.Application.Configuration;
using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Application.Interfaces;
using ExpenseTracker.Domain.Identity;
using Microsoft.AspNetCore.Identity;
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

        public AuthService(UserManager<ApplicationUser> userManager, IOptions<JwtSettings> jwtOptions, IEmailService emailService)
        {
            _userManager = userManager;
            _jwtSettings = jwtOptions.Value;
            _emailService = emailService;
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
                        Message = "User already exists but email is uncorfimed. A new confirmation email has been sent."
                    };
                }

                throw new Exception("Registration failed: Email is already in use");
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
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"Registration failed: {errors}");
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
            var confirmationLink = $"http://localhost:4200/confirm-email?userId={user.Id}&token={encodedToken}";

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

            if (!await _userManager.IsEmailConfirmedAsync(user))
            {
                throw new UnauthorizedAccessException("You must confirm your email before logging in.");
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
    }
}
