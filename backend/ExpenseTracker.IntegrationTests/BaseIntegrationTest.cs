using Xunit;
using ExpenseTracker.Application.DTOs;
using ExpenseTracker.Domain.Identity;
using ExpenseTracker.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using System.Data.Common;
using System.Net.Http.Headers;
using System.Net.Http.Json;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace ExpenseTracker.IntegrationTests
{
    // This tells XUnit to provide the WebApplicationFactory to this class
    public abstract class BaseIntegrationTest : IClassFixture<CustomWebApplicationFactory<Program>>, IAsyncLifetime
    {
        protected readonly HttpClient Client;
        protected readonly CustomWebApplicationFactory<Program> Factory;
        private Respawner _respawner = default!;
        private DbConnection _connection = default!;

        // This constructor is now compatible with inheritance
        protected BaseIntegrationTest(CustomWebApplicationFactory<Program> factory)
        {
            Factory = factory;
            Client = factory.CreateClient();
        }

        public async Task InitializeAsync()
        {
            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.EnsureCreatedAsync();

            var configuration = Factory.Services.GetRequiredService<IConfiguration>();
            _connection = new NpgsqlConnection(configuration.GetConnectionString("DefaultConnection"));
            await _connection.OpenAsync();

            _respawner = await Respawner.CreateAsync(_connection, new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = new[] { "public" },
                TablesToIgnore = new[]
                {
                    new Respawn.Graph.Table("__EFMigrationsHistory"),
                    new Respawn.Graph.Table("Categories")
                }
            });

            await _respawner.ResetAsync(_connection);
        }

        public async Task DisposeAsync()
        {
            if (_connection != null)
            {
                await _connection.CloseAsync();
                await _connection.DisposeAsync();
            }
        }

        protected void Authenticate(string token)
        {
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        protected async Task<string> RegisterAndLoginAsync(string email, string password, string name = "Test User")
        {
            var registerDto = new RegisterDto { Name = name, Email = email, Password = password, ConfirmPassword = password };
            var registerResponse = await Client.PostAsJsonAsync("/api/auth/register", registerDto);
            registerResponse.EnsureSuccessStatusCode();

            // Manual confirmation logic with robust retrieval
            using (var scope = Factory.Services.CreateScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                
                ApplicationUser? user = null;
                int retryCount = 0;
                while (user == null && retryCount < 3)
                {
                    user = await userManager.FindByEmailAsync(email);
                    if (user == null)
                    {
                        await Task.Delay(100);
                        retryCount++;
                    }
                }

                Assert.True(user != null, "User registration failed to persist in the database.");
                
                var token = await userManager.GenerateEmailConfirmationTokenAsync(user!);
                await userManager.ConfirmEmailAsync(user!, token);
            }

            var loginDto = new { Email = email, Password = password };
            var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", loginDto);
            var authData = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

            Authenticate(authData!.Token);
            return authData.Token;
        }
    }
}
