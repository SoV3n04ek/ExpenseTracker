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
            // Arrange
            await RegisterAndLoginAsync("summary@test.com", "Password123!");

            // Act
            var response = await Client.GetAsync("/api/expenses/summary");
            var json = await response.Content.ReadAsStringAsync();

            // Assert
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
            // Range
            await RegisterAndLoginAsync("range@test.com", "Password123!");

            // Create one expense inside the range, one outside
            var inRange = new {Description = "Inside", Amount = 50m, Date = DateTimeOffset.Parse("2025-01-10"), CategoryId = 1 };
            var outOfRange = new { Description = "Outside", Amount = 1000m, Date = DateTimeOffset.Parse("2024-12-01"), CategoryId = 1 };


            await Client.PostAsJsonAsync("/api/expenses", inRange);
            await Client.PostAsJsonAsync("/api/expenses", outOfRange);

            // Act
            // Request range: Jan 1st to Jan 20th 2025
            var response = await Client.GetAsync("/api/expenses/summary?startDate=2025-01-01&endDate=2025-01-20");
            var result = await response.Content.ReadFromJsonAsync<ExpenseSummaryDto>();

            // Assert
            Assert.Equal(50, result!.TotalAmount);
            Assert.Single(result.Categories);
        }
    }
}
