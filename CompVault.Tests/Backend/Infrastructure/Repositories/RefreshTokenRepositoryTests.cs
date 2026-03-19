using CompVault.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CompVault.Tests.Backend.Infrastructure.Repositories;

public class RefreshTokenRepository
{
    // Mocker AppDbContext og setter opp systemet for testing
    private readonly AppDbContext _context;
    private readonly RefreshTokenRepository _sut;

    public RefreshTokenRepository()
    {
        // Setter opp InMemoryDatabase
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _sut = new RefreshTokenRepository(_context);
    }
}