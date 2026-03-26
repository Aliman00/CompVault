using CompVault.Frontend;
using CompVault.Frontend.Extensions;
using MudBlazor.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Razor Components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Serilog
builder.AddSerilogLogging();

// Eksisterende Http-klienter
builder.Services.AddHttpClients(builder.Configuration);

// MudBlazor
builder.Services.AddMudServices();

// Forretningslogikk
builder.Services.AddFrontendServices();

// 🚨 KONTROLLERE + BACKEND API
builder.Services.AddControllers();
builder.Services.AddHttpClient("BackendApi", client =>
{
    client.BaseAddress = new Uri("http://localhost:5010/");
});

WebApplication app = builder.Build();

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Controllers mapping
app.MapControllers();

app.Run();