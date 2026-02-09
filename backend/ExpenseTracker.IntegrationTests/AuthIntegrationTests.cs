using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Domain.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using ExpenseTracker.IntegrationTests.Helpers;

namespace ExpenseTracker.IntegrationTests
{
    public class AuthIntegrationTests : BaseIntegrationTest
    {
        public AuthIntegrationTests(CustomWebApplicationFactory<Program> factory)
            : base(factory) { }

        [Fact]
        public async Task Register_WithLargeEmail_ReturnsBadRequest()
        {
            // Arrange
            var registerDto = TestObjectFactory.GetRegisterDto("a".PadLeft(101, 'a') + "@test.com");

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/register", registerDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Register_DuplicateEmail_ReturnsBadRequest()
        {
            // Arrange
            var email = TestObjectFactory.GetUniqueEmail();
            var registerDto = TestObjectFactory.GetRegisterDto(email);

            // 1. First registration
            var resp1 = await Client.PostAsJsonAsync("/api/auth/register", registerDto);
            resp1.EnsureSuccessStatusCode();

            // 2. Manually confirm the user to ensure the second registration hits the "Email in use" block
            using (var scope = Factory.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ExpenseTracker.Domain.Identity.ApplicationUser>>();
                var user = await userManager.FindByEmailAsync(email);
                var token = await userManager.GenerateEmailConfirmationTokenAsync(user!);
                await userManager.ConfirmEmailAsync(user!, token);
            }

            // 3. Act - Second registration with same email
            var response = await Client.PostAsJsonAsync("/api/auth/register", registerDto);

            // 4. Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Email is already in use");
        }

        [Fact]
        public async Task ForgotPassword_ValidEmail_ReturnsOk()
        {
            // Arrange
            var email = TestObjectFactory.GetUniqueEmail();
            await RegisterAndLoginAsync(email, TestObjectFactory.GetSecurePassword());

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/forgot-password", new { Email = email });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ForgotPassword_RateLimit_Returns429AfterExceedingLimit()
        {
            // Use WithWebHostBuilder to override settings for this specific test
            var customClient = Factory.WithWebHostBuilder(builder =>
            {
                // Set a very low limit for this isolated test
                builder.UseSetting("RateLimit:PermitLimit", "2");
                builder.UseSetting("RateLimit:WindowMinutes", "1");
            }).CreateClient();

            var email = TestObjectFactory.GetUniqueEmail();

            // First request
            await customClient.PostAsJsonAsync("/api/auth/forgot-password", new { Email = email });
            // Second request
            await customClient.PostAsJsonAsync("/api/auth/forgot-password", new { Email = email });
            // Third request - should hit the limit (set to 2)
            var response = await customClient.PostAsJsonAsync("/api/auth/forgot-password", new { Email = email });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
            var retryAfter = response.Headers.RetryAfter;
            retryAfter.Should().NotBeNull();
        }

        [Fact]
        public async Task PasswordResetFlow_Succeeds()
        {
            // 1. Arrange - Register a user
            var email = TestObjectFactory.GetUniqueEmail();
            var password = TestObjectFactory.GetSecurePassword();
            await RegisterAndLoginAsync(email, password);

            // 2. Act - Request Password Reset
            await Client.PostAsJsonAsync("/api/auth/forgot-password", new { Email = email });

            // 3. Get token from DB and ENCODE it as the UI would
            using var scope = Factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ExpenseTracker.Domain.Identity.ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);
            var token = await userManager.GeneratePasswordResetTokenAsync(user!);
            
            var encodedToken = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(
                System.Text.Encoding.UTF8.GetBytes(token));

            // 4. Act - Reset Password
            var newPassword = "NewSecurePassword123!";
            var resetDto = new ResetPasswordRequest
            {
                Email = email,
                Token = encodedToken,
                NewPassword = newPassword,
                ConfirmPassword = newPassword
            };

            var resetResponse = await Client.PostAsJsonAsync("/api/auth/reset-password", resetDto);

            // 5. Assert
            resetResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // 6. Verify we can login with the new password
            var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new LoginDto { Email = email, Password = newPassword });
            loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ResetPassword_InvalidToken_ReturnsBadRequest()
        {
            // Arrange
            var resetDto = new ResetPasswordRequest
            {
                Email = TestObjectFactory.GetUniqueEmail(),
                Token = "InvalidToken",
                NewPassword = TestObjectFactory.GetSecurePassword(),
                ConfirmPassword = TestObjectFactory.GetSecurePassword()
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/reset-password", resetDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Register_WithPasswordExactly64_Succeeds()
        {
            // Arrange
            var longPassword = "P" + new string('a', 61) + "1!"; // 64 chars, 1 Upper, 1 Digit, 1 Special
            var registerDto = new RegisterDto
            {
                Name = "Long Pass User",
                Email = TestObjectFactory.GetUniqueEmail(),
                Password = longPassword,
                ConfirmPassword = longPassword
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/register", registerDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Register_WithCommonPassword_ReturnsBadRequest()
        {
            // Arrange
            var registerDto = TestObjectFactory.GetRegisterDto();
            registerDto = registerDto with { Password = "password", ConfirmPassword = "password" }; // Too simple

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/register", registerDto);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            
            // Check for either the FluentValidation message OR the Identity message
            content.Should().MatchRegex("Password must contain at least one digit|Passwords must have at least one non alphanumeric character");
        }
    }
}
