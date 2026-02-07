using ExpenseTracker.Application.DTOs;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ExpenseTracker.IntegrationTests
{
    public class ExpenseControllerTests : BaseIntegrationTest
    {
        public ExpenseControllerTests(CustomWebApplicationFactory<Program> factory)
            : base(factory) { }

        [Fact]
        public async Task GetExpenses_WithoutToken_Returns401Unauthorized()
        {
            Client.DefaultRequestHeaders.Authorization = null;
            var response = await Client.GetAsync("/api/expenses");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateExpense_WithValidData_ReturnsCreatedAndPersistsData()
        {
            await RegisterAndLoginAsync("tester_final@test.com", "Password123!");

            var newExpense = new CreateExpenseDto(
                "Lunch with Team",
                45.50m,
                DateTimeOffset.UtcNow,
                1
            );

            var postResponse = await Client.PostAsJsonAsync("/api/expenses", newExpense);
            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            var getResponse = await Client.GetAsync("/api/expenses");
            var pagedResult = await getResponse.Content.ReadFromJsonAsync<PagedResponse<ExpenseDto>>();

            Assert.NotNull(pagedResult);
            Assert.Contains(pagedResult.Items, e => e.Description == "Lunch with Team");
        }
    }
}