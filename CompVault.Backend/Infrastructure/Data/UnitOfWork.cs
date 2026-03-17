using CompVault.Shared.Result;

namespace CompVault.Backend.Infrastructure.Data;

/// <summary>
/// Tynn wrapper rundt <see cref="AppDbContext"/> som eksponerer <c>SaveChangesAsync</c>
/// via <see cref="IUnitOfWork"/>. Registreres som scoped i DI-containeren.
/// </summary>
public sealed class UnitOfWork(AppDbContext dbContext, ILogger<UnitOfWork> logger) : IUnitOfWork
{
    /// <inheritdoc />
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
    
    
    /// <inheritdoc />
    public async Task<Result> ExecuteInTransactionAsync(Func<Task<Result>> operation,
        CancellationToken ct = default)
    {
        // Starter transaksjonen
        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
        try
        {
            // Får et result-objekt etter en operasjon
            var result = await operation();
            
            // er dette Result-objektet en Failure, roll tilbake
            if (result.IsFailure)
                return result;
            
            // Alt er vellykket. Lagre og commit transaksjonen
            await SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return result;

        }
        catch (Exception ex) // Rollback i transkasjonen, og generer en default melding
        {
            logger.LogError(ex, "Transaction failed unexpectedly. Rolling back.");
            return Result.Failure(
                AppError.Create(ErrorCode.InternalError, "An unexpected error occured. Try again."));
        }
    } 
    
    /// <inheritdoc />
    public async Task<Result<T>> ExecuteInTransactionAsync<T>(Func<Task<Result<T>>> operation,
        CancellationToken ct = default)
    {
        // Starter transaksjonen
        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
        try
        {
            // Får et result-objekt etter en operasjon
            var result = await operation();
            
            // er dette Result-objektet en Failure, roll tilbake
            if (result.IsFailure)
                return result;
            
            // Alt er vellykket. Lagre og commit transaksjonen
            await SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return result;

        }
        catch (Exception ex) // Rollback i transkasjonen, og generer en default melding
        {
            logger.LogError(ex, "Transaction failed unexpectedly. Rolling back.");
            return Result<T>.Failure(
                AppError.Create(ErrorCode.InternalError, "An unexpected error occured. Try again."));
        }
    }   
}
