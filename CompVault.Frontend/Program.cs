using CompVault.Frontend;
using CompVault.Frontend.Extensions;
using MudBlazor.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════════════════════════
// 1. BLAZOR + INTERACTIVE RENDERING
// ═══════════════════════════════════════════════════════════════════════════════
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ═══════════════════════════════════════════════════════════════════════════════
// 2. LOGGING (Serilog)
builder.AddSerilogLogging();

// ═══════════════════════════════════════════════════════════════════════════════
// 3. MUD-BLAZOR UI-KOMPONENTER
builder.Services.AddMudServices();

// ═══════════════════════════════════════════════════════════════════════════════
// 4. HTTP-KLIENTER
// - AddHttpClients() = dine eksisterende klienter
// - BackendApi = for OTP-backend
builder.Services.AddHttpClients(builder.Configuration);
builder.Services.AddHttpClient("BackendApi", client =>
{
    client.BaseAddress = new Uri("http://localhost:5010/");  // Backend URL
});

// ═══════════════════════════════════════════════════════════════════════════════
// 5. API CONTROLLERS (for fremtidig bruk)
builder.Services.AddControllers();

// ═══════════════════════════════════════════════════════════════════════════════
// 6. FORETNINGSLOGIKK (dine services)
builder.Services.AddFrontendServices();

WebApplication app = builder.Build();

// ═══════════════════════════════════════════════════════════════════════════════
// 7. HTTP PIPELINE
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();  // CSS/JS-filer
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapControllers();  // API-routes

app.Run();