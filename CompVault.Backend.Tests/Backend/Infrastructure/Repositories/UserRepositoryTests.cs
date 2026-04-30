using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Repositories.Identity;
using CompVault.Backend.Tests.Common;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Tests.Backend.Infrastructure.Repositories;

public class UserRepositoryTests : IDisposable
{
    // In-memory database og systemet vi tester
    private readonly AppDbContext _dbContext;
    private readonly UserRepository _sut;

    // Testbrukere som brukes på tvers av testene
    private readonly ApplicationUser _activeUser = new()
    {
        Id = Guid.NewGuid(),
        Email = "active@example.com",
        UserName = "active@example.com",
        FirstName = "Aktiv",
        LastName = "Bruker",
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    private readonly ApplicationUser _deletedUser = new()
    {
        Id = Guid.NewGuid(),
        Email = "deleted@example.com",
        UserName = "deleted@example.com",
        FirstName = "Slettet",
        LastName = "Bruker",
        IsActive = false,
        DeletedAt = DateTime.UtcNow.AddDays(-1),
        CreatedAt = DateTime.UtcNow.AddDays(-10)
    };

    public UserRepositoryTests()
    {
        // Hver test får sin egen isolerte in-memory database
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(options, new BypassDepartmentScopeService());
        _sut = new UserRepository(_dbContext);

        // Seeder testdata
        // Task.Run brukes siden xUnit ikke støtter async konstruktører
        Task.Run(async () =>
        {
            _dbContext.Users.AddRange(_activeUser, _deletedUser);
            await _dbContext.SaveChangesAsync();
        }).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Tester at GetByIdAsync finner en bruker som finnes
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_WhenUserExists_ReturnsUser()
    {
        // Act
        ApplicationUser? result = await _sut.GetByIdAsync(_activeUser.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_activeUser.Email, result.Email);
    }

    /// <summary>
    /// Tester at GetByIdAsync returnerer null når brukeren ikke eksisterer
    /// </summary>
    [Fact]
    public async Task GetByIdAsync_WhenUserDoesNotExist_ReturnsNull()
    {
        // Act
        ApplicationUser? result = await _sut.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Tester at GetActiveUsersAsync kun returnerer aktive brukere — ikke slettede
    /// </summary>
    [Fact]
    public async Task GetActiveUsersAsync_ReturnsOnlyActiveUsers()
    {
        // Act
        IReadOnlyList<ApplicationUser> result = await _sut.GetActiveUsersAsync();

        // Assert - Skal kun inneholde aktiv bruker, ikke den slettede
        Assert.Single(result);
        Assert.Equal(_activeUser.Email, result[0].Email);
    }

    /// <summary>
    /// Tester at GetByEmailAsync finner bruker på e-post
    /// </summary>
    [Fact]
    public async Task GetByEmailAsync_WhenEmailExists_ReturnsUser()
    {
        // Act
        ApplicationUser? result = await _sut.GetByEmailAsync(_activeUser.Email!);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_activeUser.Id, result.Id);
    }

    /// <summary>
    /// Tester at GetByEmailAsync returnerer null når e-posten ikke eksisterer
    /// </summary>
    [Fact]
    public async Task GetByEmailAsync_WhenEmailDoesNotExist_ReturnsNull()
    {
        // Act
        ApplicationUser? result = await _sut.GetByEmailAsync("ikkeeksisterende@example.com");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Tester at SoftDeleteAsync setter DeletedAt og IsActive = false uten å fjerne raden
    /// </summary>
    [Fact]
    public async Task SoftDeleteAsync_SetsDeletedAtAndDeactivatesUser()
    {
        // Arrange
        ApplicationUser? userToDelete = await _sut.GetByIdAsync(_activeUser.Id);

        // Act
        await _sut.SoftDeleteAsync(userToDelete!);
        await _dbContext.SaveChangesAsync(); // Speiler virkeligheten — endringer må persisteres

        // Assert
        ApplicationUser? userInDb = await _dbContext.Users.FindAsync(_activeUser.Id);
        Assert.NotNull(userInDb);
        Assert.False(userInDb.IsActive);
        Assert.NotNull(userInDb.DeletedAt);
    }

    /// <summary>
    /// Tester at AddAsync lagrer en ny bruker i databasen
    /// </summary>
    [Fact]
    public async Task AddAsync_AddsUserToDatabase()
    {
        // Arrange
        var newUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "ny@example.com",
            UserName = "ny@example.com",
            FirstName = "Ny",
            LastName = "Bruker",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _sut.AddAsync(newUser);
        await _dbContext.SaveChangesAsync();

        // Assert
        ApplicationUser? userInDb = await _dbContext.Users.FindAsync(newUser.Id);
        Assert.NotNull(userInDb);
        Assert.Equal("ny@example.com", userInDb.Email);
    }

    // Rydder opp in-memory databasen etter hver test
    public void Dispose() => _dbContext.Dispose();
}