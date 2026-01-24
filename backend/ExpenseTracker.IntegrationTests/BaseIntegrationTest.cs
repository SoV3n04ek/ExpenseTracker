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

namespace ExpenseTracker.IntegrationTests;

public class BaseIntegrationTest : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    protected readonly HttpClient Client;
    protected readonly WebApplicationFactory<Program> Factory;
    private Respawner _respawner = default!;
    private DbConnection _connection = default!;

    public BaseIntegrationTest(WebApplicationFactory<Program> factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Ensure database is created once
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
                new Respawn.Graph.Table("Categories") // Keep seeded categories
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
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    protected async Task ConfirmUserEmailAsync(string email)
    {
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        if (user != null)
        {
            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            await userManager.ConfirmEmailAsync(user, token);
        }
    }

    protected async Task<string> RegisterAndLoginAsync(string email, string password, string name = "Test User")
    {
        var registerDto = new { Name = name, Email = email, Password = password, ConfirmPassword = password };
        await Client.PostAsJsonAsync("/api/auth/register", registerDto);

        await ConfirmUserEmailAsync(email);

        var loginDto = new { Email = email, Password = password };
        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", loginDto);
        var authData = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        Authenticate(authData!.Token);

        return authData.Token;
    }
}