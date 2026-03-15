using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Email;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace CompVault.Tests.Backend.Integrations;

public class BackendWebApplicationFactory : WebApplicationFactory<Program>
{
    // Vi må eksponere EmailService for å sikre at vi ikke sender ekte eposter
    public Mock<IEmailService> EmailServiceMock { get; } = new();

    // protected override void ConfigureWebHot(IWebHostBuilder builder)
    // {
    //     builder.UseEnvironment("Testing");
    //     
    //     builder.ConfigureAppConfiguration(services =>
    //     {
    //         services.RemoveAll<DbContextOptions<AppDbContext>>();
    //         services.AddDbContext<AppDbC
    //     })
    //     
    // }
}