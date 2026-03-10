using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi;
using Serilog;

namespace CompVault.Backend.Infrastructure.Extensions;

/// <summary>
/// Extension-metoder på <see cref="WebApplicationBuilder"/> for oppsett av applikasjonen.
/// </summary>
public static  class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Setter opp Serilog med consolelogging
    /// </summary>
    public static void ConfigureLogging(this WebApplicationBuilder builder)
    {
        // Fjerner Microsoft standard logging.
        builder.Logging.ClearProviders();
        
        // Setter opp Serilog med kun Console-logging
        builder.Host.UseSerilog((context, config) =>
        {
            config.ReadFrom.Configuration(context.Configuration);
            config.WriteTo.Console();
        });
    }
    
    /// <summary>
    /// Konfigurerer Swagger med mulighet for Bearer Token og synlighet av Summary til metodene
    /// </summary>
    public static void ConfigureSwagger(this WebApplicationBuilder builder)
    {
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        { 
            // Lager er jwtSecurityScheme
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
                // Lager en refereanse slik at alle endepunkter med [Authorize] refe rer til samme oppsett, eller så fyller
                // det seg opp med slike oppsett pr endepunkt
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
                    new OpenApiSecuritySchemeReference(
                        JwtBearerDefaults.AuthenticationScheme,
                        document),
                    []
                }
            });
        });
    }
    
    
}