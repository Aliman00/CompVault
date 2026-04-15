using CompVault.Backend.Infrastructure.FileStorage;
using CompVault.Shared.Result;

namespace CompVault.Backend.Features.Documents.Services;

/// <inheritdoc />
public sealed class DocumentFileService(IFileStorageService fileStorage) : IDocumentFileService
{
    /// <inheritdoc />
    public async Task<(string FilePath, string Checksum)> SaveWithChecksumAsync(
        Stream stream, string relativePath, CancellationToken cancellationToken = default)
    {
        // Reset-stream posisjon i tilfelle consumeren har lest streamen før lagring
        if (stream.Position != 0)
            stream.Position = 0;

        await fileStorage.SaveAsync(stream, relativePath, cancellationToken);
        string checksum = await fileStorage.ComputeChecksumAsync(relativePath, cancellationToken);

        return (relativePath, checksum);
    }

    /// <inheritdoc />
    public Task MoveAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken = default)
        => fileStorage.MoveAsync(sourcePath, destinationPath, cancellationToken);

    /// <inheritdoc />
    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
        => fileStorage.DeleteAsync(relativePath, cancellationToken);

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken = default)
        => fileStorage.ExistsAsync(relativePath, cancellationToken);

    /// <inheritdoc />
    public Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
        => fileStorage.OpenReadAsync(relativePath, cancellationToken);

    /// <inheritdoc />
    public Result ValidateMimeType(string contentType, string[] allowedMimeTypes)
    {
        if (allowedMimeTypes.Length == 0)
            return Result.Failure(AppError.Create(ErrorCode.Validation,
                "Denne dokumenttypen har ingen tillatte filtyper konfigurert. Kontakt administrator."));

        if (!allowedMimeTypes.Contains(contentType))
            return Result.Failure(AppError.Create(ErrorCode.Validation,
                $"Filtypen '{contentType}' er ikke tillatt for denne dokumenttypen."));

        return Result.Success();
    }

    /// <inheritdoc />
    public Result ValidateFileSize(long fileSize, long maxFileSizeBytes)
    {
        if (maxFileSizeBytes > 0 && fileSize > maxFileSizeBytes)
            return Result.Failure(AppError.Create(ErrorCode.Validation,
                $"Filen er for stor. Maks tillatt størrelse: {maxFileSizeBytes / (1024 * 1024)}MB."));

        return Result.Success();
    }
}