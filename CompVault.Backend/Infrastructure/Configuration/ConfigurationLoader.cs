namespace CompVault.Backend.Infrastructure.Configuration;

/// <summary>
/// Laster inn miljøvariabler fra .env-fil før applikasjonen starter.
/// Må kalles før WebApplication.CreateBuilder slik at IConfiguration plukker opp verdiene.
/// </summary>
public static class ConfigurationLoader
{
    public static void LoadEnvironmentFile()
    {
        string envPath = Environment.GetEnvironmentVariable("COMPVAULT_ENV_FILE")
            ?? FindEnvFile();

        if (File.Exists(envPath))
            DotNetEnv.Env.Load(envPath);
    }

    // Leter oppover i mappestrukturen fra kjørekatalogen.
    private static string FindEnvFile()
    {
        string? directory = AppContext.BaseDirectory;

        while (!string.IsNullOrEmpty(directory))
        {
            string candidate = Path.Combine(directory, ".env");
            if (File.Exists(candidate))
                return candidate;

            directory = Directory.GetParent(directory)?.FullName;
        }

        return Path.Combine(Directory.GetCurrentDirectory(), ".env");
    }
}
