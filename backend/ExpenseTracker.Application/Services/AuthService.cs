using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Application.Interfaces;
using ExpenseTracker.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration; // to read JWT secret
using Microsoft.IdentityModel.Tokens;     // for security key
using System.IdentityModel.Tokens.Jwt;    // for JwtSecurityToken
using System.Security.Claims;
using System.Text;

namespace ExpenseTracker.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;

        public AuthService(UserManager<ApplicationUser> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
        {
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

            return await GenerateAuthResponse(user);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            {
                throw new UnauthorizedAccessException("Invalid email or password");
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
            var jwtKey = _configuration["Jwt:Key"] ??
                throw new InvalidOperationException("Jwt key is not configured.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

            // 3. Set expiration and signing credentials
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiration = DateTime.Now.AddMinutes(Convert.ToDouble(_configuration["Jwt:ExpireMinutes"] ?? "60"));

            // 4. Create the token structure
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
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
    }
}
