namespace CompVault.Backend.Infrastructure.Data;

/// <summary>
/// Tynn wrapper rundt <see cref="AppDbContext"/> som eksponerer <c>SaveChangesAsync</c>
/// via <see cref="IUnitOfWork"/>. Registreres som scoped i DI-containeren.
/// </summary>
public sealed class UnitOfWork(AppDbContext dbContext) : IUnitOfWork
{
    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
