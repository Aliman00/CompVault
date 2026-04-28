using CompVault.Backend.Features.Departments.Services;
using CompVault.Backend.Infrastructure.Configuration;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace CompVault.Backend.Infrastructure.Data;

/// <summary>
/// For å kunne kjøre migrasjons og kommandoer som dotnet ef database update uten å kræsje så må vi injecte en
/// DepartmentScope inn i AppDbContext noe vi ikke har tilgang til under runtime. Vi henter inn appsettings
/// for connection strings og annet for å ha tilkobling til databasen
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        ConfigurationLoader.LoadEnvironmentFile();
        
        IConfigurationRoot config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        
        DatabaseSettings dbSettings = config
            .GetSection(DatabaseSettings.SectionName)
            .Get<DatabaseSettings>()
            ?? throw new InvalidOperationException("Database-konfigurasjon mangler i appsettings.json.");

        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(
                dbSettings.BuildConnectionString(),
                npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .Options;

        return new AppDbContext(options, new DesignTimeDepartmentScope());
    }
}

file sealed class DesignTimeDepartmentScope : IDepartmentScopeService
{
    public bool HasBypass(string readAllPermissions) => true;
    
    public IReadOnlyList<Guid> GetAllowedDepartmentIds(string? readSubPermission = null) 
        => [];

    public bool IsAllowed(Guid departmentId, string readAllPermission, string? readSubPermission = null) 
        => true;
}