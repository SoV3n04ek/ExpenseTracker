using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Domain.Identity;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ExpenseTracker.IntegrationTests
{
    public class BaseIntegrationTest : IClassFixture<WebApplicationFactory<Program>>
    {
        protected readonly HttpClient Client;
        protected readonly WebApplicationFactory<Program> Factory;

        public BaseIntegrationTest(WebApplicationFactory<Program> factory)
        {
            Factory = factory;
            Client = factory.CreateClient();

            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        }

        // Helper to simulate a logged-in user
        protected void Authenticate(string token)
        {
            Client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        protected async Task ConfirmUserEmailAsync(string email)
        {
            using var scope = Factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user!);
            await userManager.ConfirmEmailAsync(user!, token);
        }

        protected async Task<string> RegisterAndLoginAsync(string email, string password, string name = "Test User")
        {
            // 1. Register
            var registerDto = new { Name = name, Email = email, Password = password, ConfirmPassword = password };
            await Client.PostAsJsonAsync("/api/auth/register", registerDto);

            // 2. Confirm Email (Internal Database logic)
            using (var scope = Factory.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var user = await userManager.FindByEmailAsync(email);
                var token = await userManager.GenerateEmailConfirmationTokenAsync(user!);
                await userManager.ConfirmEmailAsync(user!, token);
            }

            // 3. Login
            var loginDto = new { Email = email, Password = password };
            var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", loginDto);
            var authData = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

            // 4. Set Header for future requests
            Authenticate(authData!.Token);

            return authData.Token;
        }
    }
}
