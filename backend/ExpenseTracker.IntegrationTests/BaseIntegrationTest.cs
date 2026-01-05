using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Headers;

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
        }

        // Helper to simulate a logged-in user
        protected void Authenticate(string token)
        {
            Client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
    }
}
