using CompVault.Shared.DTOs.Auth;
using CompVault.Tests.Common;

namespace CompVault.Tests.Backend.Integrations.Controllers;

/// <summary>
/// IClassFixture sikrer at vi ikke oppretter flere instanser av applikasjonen
/// Siden xUnit-konstruktøren kan ikke være async så oppretter vi to hooks per test:
/// En InitializeAsync som kjører for hver test, og en DisposeAsync for å rydde opp
/// </summary>
public class AuthControllerTests(BackendWebApplicationFactory factory)
    : IClassFixture<BackendWebApplicationFactory>, IAsyncLifetime
{
    // Oppretter en Httpclient for å sende forespørsler mot endepunktene våre
    private readonly HttpClient _client = factory.CreateClient();
    
    // Initialiserer InMemory-databsen og rydder opp etter AuthController sin kjøring
    public async Task InitializeAsync() => await TestDataSeeder.SeedUsersAsync(factory.Services);
    public async Task DisposeAsync() => await TestDataSeeder.ClearDatabaseAsync(factory.Services);
    
    // -------------------------------------------------------------------------
    // POST /api/auth/request-otp
    // -------------------------------------------------------------------------
    
    /// <summary>
    /// Tester happypath. Sjekker at 
    /// </summary>
    [Fact]
    public async Task RequestOtp_ExistingEmailWithNoExistingCode_Returns200()
    {
        // Arrange
       
    }
}