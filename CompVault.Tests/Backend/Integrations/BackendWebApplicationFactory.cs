using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Email;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace CompVault.Tests.Backend.Integrations;

/// <summary>
/// Vi konfigurerer en WebApplicationFactory som starter hele Backend-applikasjonen vår InMemory
/// </summary>
public class BackendWebApplicationFactory : WebApplicationFactory<Program>
{
    // Vi mocker EmailService for å mocke email kall
    public Mock<IEmailService> EmailServiceMock { get; } = new();
    
    /// <summary>
    /// Overstyrer tjenester i Program.cs før applikasjonen starter.
    /// Her fjerner vi  PostgreSQL-databasen og bruker InMemory
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Vi bytter ut PostgreSQL med InMemory-database
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

            // Bytter ut den ekte EmailService med mocken
            services.RemoveAll<IEmailService>();
            services.AddScoped<IEmailService>(_ => EmailServiceMock.Object);
        });
    }
}