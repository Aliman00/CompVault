using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Sjekk at JWT Secret er konfigurert før vi starter opp applikasjonen. 
// Dette er kritisk for sikkerheten, og det er bedre å feile tidlig enn å kjøre med en svak eller hardkodet secret.
// TODO: Fjern denne sjekken når du har konfigurert JWT Secret i appsettings.json eller environment variables.
// string? jwtSecret = builder.Configuration["JwtSettings:Secret"];
// if (string.IsNullOrEmpty(jwtSecret) || jwtSecret.Contains("CHANGE_ME"))
// {
//     throw new InvalidOperationException(
//         "JWT Secret er ikke konfigurert! Sett JwtSettings:Secret via environment variable eller secrets.");
// }

builder.ConfigureSwagger();
builder.ConfigureLogging();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("db");
builder.Services.AddInfrastructure();
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddAuth(builder.Configuration);
builder.Services.AddEmail(builder.Configuration);
builder.Services.AddRepositories();
builder.Services.AddApplicationServices();

WebApplication app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}


app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();