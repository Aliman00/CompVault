using CompVault.Frontend;
using CompVault.Frontend.Extensions;
using CompVault.Frontend.Features.Auth.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddFrontendServices();
builder.Services.AddScoped<AuthService>();  // Legg denne INN her!

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();  // ✅ Behold denne
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();  // ✅ Kun ÉN app.Run()