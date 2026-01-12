using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Domain.Identity;
using FluentAssertions;
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
            // Arrange
            await RegisterAndLoginAsync("int@test.com", "Password123!");

            // Act
            var expenseDto = new { Description = "Integration Test Expense", Amount = 10.5m, Date = DateTimeOffset.UtcNow, CategoryId = 1 };
            var expenseResponse = await Client.PostAsJsonAsync("/api/expenses", expenseDto);

            // Assert 
            Assert.Equal(HttpStatusCode.Created, expenseResponse.StatusCode);
        }

        [Fact]
        public async Task GetSummary_ReturnsValidJsonSchema()
        {
            // 1. Arrange - Setup a user AND some data
            await RegisterAndLoginAsync("summary@test.com", "Password123!");

            var expenseDto = new
            {
                Description = "Test Expense",
                Amount = 10m,
                Date = DateTimeOffset.UtcNow,
                CategoryId = 1
            };
            await Client.PostAsJsonAsync("/api/expenses", expenseDto);

            // 2. Act
            var response = await Client.GetAsync("/api/expenses/summary");
            var json = await response.Content.ReadAsStringAsync();

            // 3. Assert
            json.Should().Contain("\"totalAmount\":");
            json.Should().Contain("\"categoryName\":");
        }

        [Fact]
        public async Task Forbidden_UserCannotAccessOtherUsersExpense()
        {
            // Arrange
            // 1. Login as User A and create an expense
            await RegisterAndLoginAsync("userA@test.com", "Password123!", "User A");
            var expenseDto = new
            {
                Description = "User A Secret Expense",
                Amount = 100m,
                Date = DateTimeOffset.UtcNow,
                CategoryId = 1
            };

            var createResponse = await Client.PostAsJsonAsync("/api/expenses", expenseDto);
            // Use the Location header or deserialize to get the ID
            var createdExpense = await createResponse.Content.ReadFromJsonAsync<ExpenseDto>();
            var expenseIdFromA = createdExpense!.Id;

            // 2. Now login as User B
            await RegisterAndLoginAsync("userB@test.com", "Password123!", "User B");

            // Act
            // User B tries to GET User A's expense
            var getResponse = await Client.GetAsync($"/api/expenses/{expenseIdFromA}");

            // User B tries to DELETE User A's expense
            var deleteResponse = await Client.DeleteAsync($"/api/expenses/{expenseIdFromA}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);

        }

        [Fact]
        public async Task GetSummary_WithDataRange_ReturnsOnlyExpensesWithinRange()
        {
            // 1. Arrange
            await RegisterAndLoginAsync("range@test.com", "Password123!");

            // Use specific UTC dates
            var dateInRange = new DateTimeOffset(2025, 1, 10, 0, 0, 0, TimeSpan.Zero);
            var dateOutOfRange = new DateTimeOffset(2024, 12, 1, 0, 0, 0, TimeSpan.Zero);

            var inRange = new { Description = "Inside", Amount = 50m, Date = dateInRange, CategoryId = 1 };
            var outOfRange = new { Description = "Outside", Amount = 1000m, Date = dateOutOfRange, CategoryId = 1 };

            await Client.PostAsJsonAsync("/api/expenses", inRange);
            await Client.PostAsJsonAsync("/api/expenses", outOfRange);

            // 2. Act 
            // Format the dates as ISO strings so the Controller parses them correctly
            string startStr = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero).ToString("yyyy-MM-dd");
            string endStr = new DateTimeOffset(2025, 1, 20, 0, 0, 0, TimeSpan.Zero).ToString("yyyy-MM-dd");

            var response = await Client.GetAsync($"/api/expenses/summary?startDate={startStr}&endDate={endStr}");
            var result = await response.Content.ReadFromJsonAsync<ExpenseSummaryDto>();

            // 3. Assert
            Assert.Equal(50, result!.TotalAmount);
            Assert.Single(result.Categories);
        }   

        [Fact]
        public async Task GetSummary_EmptyData_ReturnsBaseKeys()
        {
            await RegisterAndLoginAsync("empty@test.com", "Password123!");
            var response = await Client.GetAsync("/api/expenses/summary");
            var json = await response.Content.ReadAsStringAsync();

            json.Should().Contain("\"totalAmount\":0");
            json.Should().Contain("\"categories\":[]");
        }

        [Fact]
        public async Task CreateExpense_InvalidData_Returns400BadRequest()
        {
            // Arrange
            await RegisterAndLoginAsync("validation@test.com", "Password123!");

            // Amount is negative, which violates our FluentValidation rule
            var invalidExpense = new
            {
                Description = "",
                Amount = -5.0m,
                Date = DateTimeOffset.UtcNow,
                CategoryId = 1
            };

            // Act
            var response = await Client.PostAsJsonAsync("/api/expenses", invalidExpense);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // Optional: Check if the error message is helpful
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Amount must be greater than zero");
        }

        [Fact]
        public async Task Login_WithWrongPassword_Returns401Unauthorized()
        {
            // Arrange
            await RegisterAndLoginAsync("tester@test.com", "CorrectPassword123!");
            var wrongLogin = new { Email = "tester@test.com", Password = "wrongPassword" };

            // Act
            var response = await Client.PostAsJsonAsync("/api/auth/login", wrongLogin);

            // Assert 
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var content = await response.Content.ReadFromJsonAsync<dynamic>();
            // content.message should be "Invalid email or password"
        }
    }
}
