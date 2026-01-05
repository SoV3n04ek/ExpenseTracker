using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace ExpenseTracker.IntegrationTests
{
    public class ExpenseIntegrationTests : BaseIntegrationTest
    {
        public ExpenseIntegrationTests(WebApplicationFactory<Program> factory)
            : base(factory) { }

        [Fact]
        public async Task UserJourney_CanRegisterAndCreateExpense()
        {
            // 1. Register (Note: In a real integration test, you'd use a TestDb)
            var registerDto = new { Name = "Int Test", Email = "int@test.com", Password = "Password123!", ConfirmPassword = "Password123!" };
            var regResponse = await Client.PostAsJsonAsync("/api/auth/register", registerDto);
            Assert.True(regResponse.IsSuccessStatusCode);

            using (var scope = Factory.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var user = await userManager.FindByEmailAsync("int@test.com");
                var token = await userManager.GenerateEmailConfirmationTokenAsync(user!);
                await userManager.ConfirmEmailAsync(user!, token);
            }

            // 2. Login
            var loginDto = new { Email = "int@test.com", Password = "Password123!" };
            var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", loginDto);
            var authData = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

            // 3. Authenticate the Client
            Authenticate(authData.Token);

            // 4. Create Expense
            var expenseDto = new { Description = "Integration Test Expense", Amount = 10.5m, Date = DateTimeOffset.UtcNow, CategoryId = 1 };
            var expenseResponse = await Client.PostAsJsonAsync("/api/expenses", expenseDto);

            // Assert 
            Assert.Equal(HttpStatusCode.Created, expenseResponse.StatusCode);
        }
    }
}
