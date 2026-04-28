using CompVault.Backend.Features.Departments.Services;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Data.Interceptors;
using CompVault.Backend.Infrastructure.Email;
using CompVault.Backend.Tests.Common;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Moq;

using Npgsql;

using Respawn;

using Testcontainers.PostgreSql;

namespace CompVault.Backend.Tests.Backend.Integrations;

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

    private Respawner _respawner = null!;

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

            foreach (ServiceDescriptor? descriptor in descriptors)
                services.Remove(descriptor);
            
            // Fjerner filterene slik at tester kan tester ikke trenger å tenke på DepartmentScopeService og hierarkiet
            services.AddDbContext<AppDbContext>((sp, options) =>
                options.UseNpgsql(_postgres.GetConnectionString())
                    .AddInterceptors(new AuditSaveChangesInterceptor(sp),
                        new DepartmentScopeSaveChangesInterceptor(sp)));

            // Bytt ut ekte scope-service med bypass i alle integrasjonstester
            services.RemoveAll<IDepartmentScopeService>();
            services.AddScoped<IDepartmentScopeService, BypassDepartmentScopeService>();

            // Bytter ut den ekte EmailService med mocken
            services.RemoveAll<IEmailService>();
            services.AddScoped<IEmailService>(_ => EmailServiceMock.Object);
        });
    }

    // Starter containeren før testene kjører
    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Seeder og migrerer databasen
        await TestDataSeeder.CreateDb(Services);

        // Hent og åpme tilkobling
        await using NpgsqlConnection npgsqlConnection = new(_postgres.GetConnectionString());
        await npgsqlConnection.OpenAsync();

        // Konfiguerer respawn slik at vi kan resette databasen til tilstanden etter seeding
        _respawner = await Respawner.CreateAsync(npgsqlConnection,
            new RespawnerOptions { DbAdapter = DbAdapter.Postgres, SchemasToInclude = ["public"] });
    }

    /// <summary>
    /// Resetter databasen til tilstanden etter initial seeding
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        await using NpgsqlConnection npgsqlConnection = new(_postgres.GetConnectionString());
        await npgsqlConnection.OpenAsync();
        await _respawner.ResetAsync(npgsqlConnection);
    }

    // Stopper containeren etter testene er ferdig
    public new async Task DisposeAsync()
        => await _postgres.DisposeAsync();
}