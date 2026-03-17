using CompVault.Shared.Result;

namespace CompVault.Backend.Infrastructure.Data;

/// <summary>
/// Håndterer databasetransaksjoner for å sikre atomiske operasjoner.
/// Brukes når flere repository-operasjoner må lykkes eller feile samlet
/// for å unngå inkonsistent databasetilstand.
/// Registreres som scoped i DI-containeren.
/// </summary>
public sealed class UnitOfWork(AppDbContext dbContext, ILogger<UnitOfWork> logger) : IUnitOfWork
{


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
            {
                await transaction.RollbackAsync(ct);
                return result;
            }

            // Alt er vellykket. Lagre og commit transaksjonen
            await SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return result;

        }
        catch (Exception ex) // Rollback i transkasjonen, og generer en default melding
        {
            logger.LogError(ex, "Transaction failed unexpectedly. Rolling back.");
            await transaction.RollbackAsync(ct);
            return Result.Failure(
                AppError.Create(ErrorCode.InternalError, "An unexpected error occurred. Try again."));
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
            {
                await transaction.RollbackAsync(ct);
                return result;
            }


            // Alt er vellykket. Lagre og commit transaksjonen
            await SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return result;

        }
        catch (Exception ex) // Rollback i transkasjonen, og generer en default melding
        {
            logger.LogError(ex, "Transaction failed unexpectedly. Rolling back.");
            await transaction.RollbackAsync(ct);
            return Result<T>.Failure(
                AppError.Create(ErrorCode.InternalError, "An unexpected error occurred. Try again."));
        }
    }

    private Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
