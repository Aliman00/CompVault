namespace CompVault.Tests.Backend.Integrations.Controllers;

/// <summary>
/// IClassFixture sikrer at vi ikke oppretter flere instanser av applikasjonen
/// Siden xUnit-konstruktøren kan ikke være async så oppretter vi to hooks per test:
/// En InitializeAsync som kjører for hver test, og en DisposeAsync for å rydde opp
/// </summary>
// public class AuthControllerTests : IClassFixture<BackendWebApplicationFactory>, IAsyncLifetime
// {
//     private readonly BackendWebApplicationFactory _factory;
//     private readonly HttpClient _client;
//     
// }