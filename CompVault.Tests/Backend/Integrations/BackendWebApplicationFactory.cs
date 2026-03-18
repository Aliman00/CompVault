using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Email;
using CompVault.Tests.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Testcontainers.PostgreSql;

namespace CompVault.Tests.Backend.Integrations;

/// <summary>
/// Vi konfigurerer en WebApplicationFactory som starter hele Backend-applikasjonen vår InMemory
/// </summary>
public class BackendWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{       
    // Starter en PostgreSQL-container for integrasjonstester. Valgt 17-alpine da den er mer testet enn 18
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("compvault_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();
    
    // Vi mocker EmailService for å mocke email kall
    public Mock<IEmailService> EmailServiceMock { get; } = new();

    /// <summary>
    /// Overstyrer tjenester i Program.cs før applikasjonen starter.
    /// Her fjerner vi  PostgreSQL-databasen og bruker InMemory
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Overstyrerer appsettings sine verdier med egne for testing
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(TestConfiguration.Default);
        });

        builder.ConfigureServices(services =>
        {
            // Fjern alle DbContext-relaterte registreringer
            var descriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                            || d.ServiceType == typeof(AppDbContext))
                .ToList();

            foreach (var descriptor in descriptors)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));

            // Bytter ut den ekte EmailService med mocken
            services.RemoveAll<IEmailService>();
            services.AddScoped<IEmailService>(_ => EmailServiceMock.Object);
        });
    }
    
    // Starter containeren før testene kjører
    public async Task InitializeAsync()
        => await _postgres.StartAsync();
    
    // Stopper containeren etter testene er ferdig
    public new async Task DisposeAsync()
        => await _postgres.DisposeAsync();
}
