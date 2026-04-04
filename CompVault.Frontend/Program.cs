using CompVault.Frontend;
using CompVault.Frontend.Extensions;
using CompVault.Frontend.Features.Auth.Services;

using MudBlazor.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// ====================== Services ======================
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.AddSerilogLogging();
builder.Services.AddHttpClients(builder.Configuration);
builder.Services.AddMudServices();

// ---------- Auth / OTP ----------
builder.Services.AddAuth(builder.Configuration, builder.Environment);
builder.Services.AddAuthPolicies();
builder.Services.AddScoped<AuthService>(); 

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddHttpContextAccessor();// <-- Legg til AuthService som scoped

builder.Services.AddFrontendServices(builder.Environment);
builder.Services.AddRazorPages();

// ====================== Build app ======================
WebApplication app = builder.Build();

// ====================== Middleware ======================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

// ============ Autentisering ============
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();



app.Run();