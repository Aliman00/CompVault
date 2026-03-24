namespace CompVault.Backend.Infrastructure.Configuration;

public static class ConfigurationValidator
{
    public static void ValidateAll()
    {
        ValidateDatabase();
        ValidateJwt();
        ValidateEmail();
    }

    private static void ValidateDatabase()
    {
        string? host = Environment.GetEnvironmentVariable("Database__Host");
        string? name = Environment.GetEnvironmentVariable("Database__Name");
        string? username = Environment.GetEnvironmentVariable("Database__Username");
        string? password = Environment.GetEnvironmentVariable("Database__Password");

        if (string.IsNullOrWhiteSpace(host))
            throw new InvalidOperationException("Database:Host er ikke konfigurert.");
        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException("Database:Name er ikke konfigurert.");
        if (string.IsNullOrWhiteSpace(username))
            throw new InvalidOperationException("Database:Username er ikke konfigurert.");
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Database:Password er ikke konfigurert.");
    }

    private static void ValidateJwt()
    {
        string? secret = Environment.GetEnvironmentVariable("JwtSettings__Secret");
        string? issuer = Environment.GetEnvironmentVariable("JwtSettings__Issuer");
        string? audience = Environment.GetEnvironmentVariable("JwtSettings__Audience");

        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("JWT Secret er ikke konfigurert.");
        if (string.IsNullOrWhiteSpace(issuer))
            throw new InvalidOperationException("JWT Issuer er ikke konfigurert.");
        if (string.IsNullOrWhiteSpace(audience))
            throw new InvalidOperationException("JWT Audience er ikke konfigurert.");
    }

    private static void ValidateEmail()
    {
        string? apiKey = Environment.GetEnvironmentVariable("Email__ApiKey");
        string? fromAddress = Environment.GetEnvironmentVariable("Email__FromAddress");

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Email:ApiKey er ikke konfigurert.");
        if (string.IsNullOrWhiteSpace(fromAddress))
            throw new InvalidOperationException("Email:FromAddress er ikke konfigurert.");
    }
}
