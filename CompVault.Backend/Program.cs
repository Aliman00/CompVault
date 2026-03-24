using CompVault.Backend.Common.Authorization;
using CompVault.Backend.Dev;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Configuration;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Extensions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Testing")
{
    ConfigurationLoader.LoadEnvironmentFile();
    ConfigurationValidator.ValidateAll();
}

builder.ConfigureSwagger();
builder.ConfigureLogging();

builder.Services.AddControllers();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("db");
builder.Services.AddInfrastructure();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuthorizationFailureHandler>();
builder.Services.AddDatabase(builder.Configuration, builder.Environment);
builder.Services.AddAuth(builder.Configuration);
builder.Services.AddEmail(builder.Configuration, builder.Environment);
builder.Services.AddRepositories();
builder.Services.AddApplicationServices();

WebApplication app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// Seed testdata kun i Development-miljøet
if (app.Environment.IsDevelopment())
{
    using IServiceScope scope = app.Services.CreateScope();
    UserManager<ApplicationUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    RoleManager<ApplicationRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
    ILogger logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DatabaseSeeder.SeedAsync(userManager, roleManager, logger);
}

app.Run();

// Eksponerer Program for integrasjonstester
public partial class Program;