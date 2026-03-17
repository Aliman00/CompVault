using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi;
using Serilog;

namespace CompVault.Backend.Infrastructure.Extensions;

/// <summary>
/// Extension-metoder på <see cref="WebApplicationBuilder"/> for oppsett av applikasjonen.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Setter opp Serilog med consolelogging fra appsettings
    /// </summary>
    public static void ConfigureLogging(this WebApplicationBuilder builder)
    {
        // Fjerner Microsoft standard logging
        builder.Logging.ClearProviders();

        // Setter opp Serilog med innstillinger fra appsettings
        builder.Host.UseSerilog((context, services, config) =>
            config.ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext());
    }

    /// <summary>
    /// Konfigurerer Swagger med mulighet for Bearer Token og synlighet av Summary til metodene
    /// </summary>
    public static void ConfigureSwagger(this WebApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            // Lager et jwtSecurityScheme
            var jwtSecurityScheme = new OpenApiSecurityScheme
            {
                // Setter det at bearer skal inneholde JWT
                BearerFormat = "JWT",
                // Egendefinert navn som vises i UI-en
                Name = "JWT Authorization",
                // Hvilken type autentiseringsmekanisme vi skal bruke, feks Http, ApiKey
                Type = SecuritySchemeType.Http,
                // Forteller hvilket scheme vi skal bruke, og JwtBearerDefaults.AuthenticationScheme = "Bearer"
                Scheme = JwtBearerDefaults.AuthenticationScheme,
                // Egendefinert beskrivelse som vises i UI-en
                Description = "Enter your JWT Access Token",
                // Vi finner tokenet i headeren
                In = ParameterLocation.Header,
            };

            // Inkluderer summary-tagger
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            options.IncludeXmlComments(xmlPath);

            // Vi registerer oppsettet med Bearer og OpenApiSecurityScheme objeektet vårt
            options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, jwtSecurityScheme);


            // Dette forteller Swagger at alle endepunkter med [Authorize]-attributen bruker JWT
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, document), []
                }
            });
        });
    }


}
