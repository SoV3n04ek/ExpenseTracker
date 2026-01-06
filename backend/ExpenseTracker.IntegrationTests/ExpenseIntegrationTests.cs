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
    }
}
