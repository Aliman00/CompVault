// Denne må være en refereranse til CompVault.Backend - ikke endre
// ReSharper disable once CheckNamespace
namespace CompVault.Backend;

/// <summary>
/// Gir oss et anker til backend slik at Test-prosjektet kan skille mellom hvem Program.cs som hører til hvert prosejkt
/// </summary>
public abstract class BackendMarker;