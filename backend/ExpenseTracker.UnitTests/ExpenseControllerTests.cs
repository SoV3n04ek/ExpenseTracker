using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;

namespace ExpenseTracker.UnitTests
{
    public class ExpenseControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public ExpenseControllerTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetExpenses_WithoutToken_Returns401Unauthorized()
        {
            // Act
            var response = await _client.GetAsync("/api/expenses");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CreateExpense_WithValidData_ReturnsCreatedAndPersistData()
        {
            var newExpense = new
            {
                Description = "Lunch with Team",
                Amount = 45.50m,
                Date = DateTimeOffset.UtcNow,
                CategoryId = 1
            };

            // Act
            var postResponse = await _client.PostAsJsonAsync("/api/expenses", newExpense);

            //Assert
            Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

            //// 4. Verify - Can we actually get the list and find our new expense?
            var getResponse = await _client.GetAsync("/api/expenses");
            var expenses = await getResponse.Content.ReadFromJsonAsync<List<dynamic>>();

            Assert.Contains(expenses, e => e.GetProperty("description").GetString() == "Lunch with Team");
        }
    }
}   
