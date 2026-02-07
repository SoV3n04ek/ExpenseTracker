using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Identity;
using FluentAssertions;
using FluentAssertions.Primitives;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace ExpenseTracker.IntegrationTests
{
    public class ExpenseIntegrationTests : BaseIntegrationTest
    {
        public ExpenseIntegrationTests(CustomWebApplicationFactory<Program> factory)
            : base(factory) { }

        [Fact]
        public async Task UserJourney_CanRegisterAndCreateExpense()
        {
            // Arrange
            await RegisterAndLoginAsync("int@test.com", "Password123!");

            // Act
            var expenseDto = new CreateExpenseDto("Integration Test Expense", 10.5m, DateTimeOffset.UtcNow, 1);
            var expenseResponse = await Client.PostAsJsonAsync("/api/expenses", expenseDto);

            // Assert 
            Assert.Equal(HttpStatusCode.Created, expenseResponse.StatusCode);
        }

        [Fact]
        public async Task GetSummary_ReturnsValidJsonSchema()
        {
            // 1. Arrange - Setup a user AND some data
            await RegisterAndLoginAsync("summary@test.com", "Password123!");

            var expenseDto = new CreateExpenseDto(
                "Test Expense",
                10m,
                DateTimeOffset.UtcNow,
                1
            );
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
            var expenseDto = new CreateExpenseDto(
                "User A Secret Expense",
                100m,
                DateTimeOffset.UtcNow,
                1
            );

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

            var inRange = new CreateExpenseDto("Inside", 50m, dateInRange, 1);
            var outOfRange = new CreateExpenseDto("Outside", 1000m, dateOutOfRange, 1);

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
            var invalidExpense = new CreateExpenseDto(
                "",
                -5.0m,
                DateTimeOffset.UtcNow,
                1
            );

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

        [Fact]
        public async Task UpdateExpense_UserCannotUpdateOtherUsersExpense_ReturnsNotFound()
        {
            // 1. Arrange - User A creates an expense
            await RegisterAndLoginAsync("userA_update@test.com", "Password123!");
            // CreateExpenseDto
            var originalExpense = new CreateExpenseDto(
                "User A Original",
                10m,
                DateTimeOffset.UtcNow,
                1
            );
            var createResponse = await Client.PostAsJsonAsync("/api/expenses", originalExpense);
            var created = await createResponse.Content.ReadFromJsonAsync<ExpenseDto>();

            // 2. Switch to User B
            await RegisterAndLoginAsync("userB_hacker@test.com", "Password123!");
            var updateDto = new CreateExpenseDto("Hacked By B", 999m, DateTimeOffset.UtcNow, 1);

            // 3. Act - User B tries to update User A's expense ID
            var updateResponse = await Client.PutAsJsonAsync($"/api/expenses/{created!.Id}", updateDto);

            // 4. Assert
            Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);

            // Verify it wasn't actually changed in the DB
            var verifyResponse = await Client.GetAsync($"/api/expenses/{created.Id}");
            Assert.Equal(HttpStatusCode.NotFound, verifyResponse.StatusCode);
        }

        [Fact]
        public async Task GetExpenses_WithoutToken_Returns401Unauthorized()
        {
            // Arrange 
            // Ensure the client has no Authorization header
            Client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await Client.GetAsync("/api/expenses");

            // Assert 
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetSummary_WithoutToken_Returns401Unauthorized()
        {
            // Arrange
            Client.DefaultRequestHeaders.Authorization = null;

            // Act
            var response = await Client.GetAsync("/api/expenses/summary");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateExpense_WithNonExistentCategory_ReturnsBadRequest()
        {
            // Arrange
            await RegisterAndLoginAsync("integrity@test.com", "Password123!");

            var expenseWithBadCategory = new CreateExpenseDto(
                "Phantom Category Expense",
                50.0m,
                DateTimeOffset.UtcNow,
                99999 // This ID should not exist 
            );

            // Act
            var response = await Client.PostAsJsonAsync("/api/expenses", expenseWithBadCategory);

            // Assert
            // If Foreign Key constraint or a Validator check, this should be 400
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UserJourney_HandleDoubleSubmitAndPreciseSummary()
        {
            // 1.  Arrange - register
            await RegisterAndLoginAsync("precision@test.com", "Password123!");

            // We use "odd" decimal numbers to test floating-point precision (decimal vs double)
            var expense1 = new CreateExpenseDto("Item 1", 10.33m, DateTimeOffset.UtcNow, 1);
            var expense2 = new CreateExpenseDto("Item 2", 20.67m, DateTimeOffset.UtcNow, 1);
            // 2. Act - Simulate a "Double submit"
            var response1 = await Client.PostAsJsonAsync("/api/expenses", expense1);
            await Task.Delay(100); // Small delay to ensure DB persistence for duplicate check
            var response2 = await Client.PostAsJsonAsync("/api/expenses", expense1);

            await Client.PostAsJsonAsync("/api/expenses", expense2);

            // Assert Double Submit results
            response1.StatusCode.Should().Be(HttpStatusCode.Created);
            response2.StatusCode.Should().Be(HttpStatusCode.Conflict);

            // 3. Act - Get Summary
            var response = await Client.GetAsync("/api/expenses/summary");
            var summary = await response.Content.ReadFromJsonAsync<ExpenseSummaryDto>();

            // 4. Assert
            // Total should be 10.33 + 20.67 = 31.00 (since double submit for expense1 was blocked)
            summary!.TotalAmount.Should().Be(31.00m);

            // Check Category Percentages
            // (20.66 / 41.33) is ~50% and (20.67 / 41.33) is ~50%
            var category = summary.Categories.FirstOrDefault(c => c.CategoryId == 1);
            category!.Percentage.Should().BeInRange(99.9, 100.1); // Should aggregate to 100% for the single category

        }

        [Fact]
        public async Task GetSummary_WithInvalidDateRange_Returns400BadRequest()
        {
            // Arrange
            await RegisterAndLoginAsync("invalidrange@test.com", "Password123!");
            var startDate = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");
            var endDate = DateTimeOffset.UtcNow.AddDays(-7).ToString("yyyy-MM-dd");

            // Act
            var response = await Client.GetAsync($"/api/expenses/summary?startDate={startDate}&endDate={endDate}");

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // Optional: Check if the error message is helpful
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Start date must be before end date");
        }
    }
}
