using Serilog;

namespace CompVault.Frontend.Extensions;

/// <summary>
/// Extension-metoder på <see cref="WebApplicationBuilder"/> for oppsett av applikasjonen.
/// </summary>
public static class WebApplicationBuilderExtensions
{

    /// <summary>
    /// Setter opp Serilog med consolelogging fra appsettings
    /// </summary>
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        // Fjerner Microsoft standard logging
        builder.Logging.ClearProviders();

        builder.Host.UseSerilog((context, services, config) =>
            config.ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext());

        return builder;
    }
}
