using Npgsql;

namespace CompVault.Backend.Infrastructure.Data;

/// <summary>
/// Databaseinnstillinger hentet fra konfigurasjon. Bindes til seksjonen "Database".
/// </summary>
public sealed class DatabaseSettings
{
    public const string SectionName = "Database";

    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 5432;
    public string Name { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Bygger en Npgsql connection string fra de individuelle feltene.
    /// Bruker NpgsqlConnectionStringBuilder for å håndtere spesialtegn i passord
    /// (f.eks. semikolon og likhetstegn) korrekt.
    /// </summary>
    public string BuildConnectionString()
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = Host,
            Port = Port,
            Database = Name,
            Username = Username,
            Password = Password
        };
        return builder.ToString();
    }
}
