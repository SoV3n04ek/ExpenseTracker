using ExpenseTracker.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace ExpenseTracker.IntegrationTests
{
    public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        public Mock<IEmailService> EmailServiceMock { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            // Use UseSetting for more direct overrides in minimal APIs
            builder.UseSetting("RateLimit:PermitLimit", "10000");
            builder.UseSetting("RateLimit:WindowMinutes", "1");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IEmailService>();
                services.AddScoped(_ => EmailServiceMock.Object);
            });
        }
    }
}
