using CompVault.Backend.Tests.Backend.Integrations;
namespace CompVault.Backend.Tests.Common;

/// <summary>
/// Sikrer at kun en test-container kjører
/// </summary>
[CollectionDefinition(nameof(IntegrationTestCollection))]
public class IntegrationTestCollection : ICollectionFixture<BackendWebApplicationFactory>;