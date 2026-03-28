using CompVault.Frontend;
using CompVault.Frontend.Extensions;

using MudBlazor.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.AddSerilogLogging();
builder.Services.AddHttpClients(builder.Configuration);
builder.Services.AddMudServices();
builder.Services.AddAuthorization();
builder.Services.AddAuth(builder.Configuration, builder.Environment);
builder.Services.AddFrontendServices(builder.Environment);
builder.Services.AddRazorPages();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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