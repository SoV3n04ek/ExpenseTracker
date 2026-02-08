using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Domain.Identity;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace ExpenseTracker.IntegrationTests
{
    public class AuthIntegrationTests : BaseIntegrationTest
    {
        public AuthIntegrationTests(CustomWebApplicationFactory<Program> factory)
            : base(factory) { }

        [Fact]
        public async Task PasswordResetFlow_Succeeds()
        {
            // 1. Arrange: Register a test user
            var email = "reset@test.com";
            var oldPassword = "OldPassword123!";
            var newPassword = "NewPassword123!";
            await RegisterAndLoginAsync(email, oldPassword, "Reset User");

            // Mock the email service to capture the reset link
            string? capturedBody = null;
            Factory.EmailServiceMock
                .Setup(x => x.SendEmailAsync(email, It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string, string>((to, subject, body) => capturedBody = body)
                .Returns(Task.CompletedTask);

            // 2. Act: Call forgot-password
            var forgotDto = new ForgotPasswordRequest { Email = email };
            var forgotResponse = await Client.PostAsJsonAsync("/api/auth/forgot-password", forgotDto);
            forgotResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Extract token and email from the captured email body
            capturedBody.Should().NotBeNull();
            // Link format: http://localhost:4200/reset-password?token={encodedToken}&email={encodedEmail}
            var tokenMatch = Regex.Match(capturedBody!, @"token=([^&]+)");
            var emailMatch = Regex.Match(capturedBody!, @"email=([^&']+)");

            tokenMatch.Success.Should().BeTrue();
            emailMatch.Success.Should().BeTrue();

            var token = tokenMatch.Groups[1].Value;
            var capturedEmailUrlEncoded = emailMatch.Groups[1].Value;
            var capturedEmail = System.Net.WebUtility.UrlDecode(capturedEmailUrlEncoded);

            capturedEmail.Should().Be(email);

            // 3. Act: Reset password
            var resetDto = new ResetPasswordRequest
            {
                Email = capturedEmail,
                Token = token,
                NewPassword = newPassword,
                ConfirmPassword = newPassword
            };
            var resetResponse = await Client.PostAsJsonAsync("/api/auth/reset-password", resetDto);
            resetResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // 4. Assert: Old login fails, new login succeeds
            // Logout (clear headers)
            Client.DefaultRequestHeaders.Authorization = null;

            var oldLogin = new { Email = email, Password = oldPassword };
            var oldLoginResponse = await Client.PostAsJsonAsync("/api/auth/login", oldLogin);
            oldLoginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            var newLogin = new { Email = email, Password = newPassword };
            var newLoginResponse = await Client.PostAsJsonAsync("/api/auth/login", newLogin);
            newLoginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ForgotPassword_RateLimit_Returns429AfterExceedingLimit()
        {
            // Arrange
            var email = "ratelimit@test.com";
            var forgotDto = new ForgotPasswordRequest { Email = email };

            // Act & Assert
            // We allow 5 requests per minute. The 6th should be rejected.
            for (int i = 1; i <= 5; i++)
            {
                var response = await Client.PostAsJsonAsync("/api/auth/forgot-password", forgotDto);
                response.StatusCode.Should().Be(HttpStatusCode.OK, $"Request {i} should be allowed");
            }

            // 6th request should return 429 Too Many Requests
            var rateLimitedResponse = await Client.PostAsJsonAsync("/api/auth/forgot-password", forgotDto);
            rateLimitedResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests, "6th request should be rate limited");

            // Verify the custom error message
            var errorBody = await rateLimitedResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            errorBody.Should().ContainKey("message");
            errorBody!["message"].Should().Be("Too many attempts. Please try again in 60 seconds.");
        }
    }
}
