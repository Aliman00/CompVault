using System.Security.Cryptography;

using CompVault.Backend.Infrastructure.FileStorage.Configuration;

using Microsoft.Extensions.Options;

namespace CompVault.Backend.Infrastructure.FileStorage;

/// <summary>
/// Lokal diskbasert fillagring. Filene lagres under en konfigurert rotmappe.
/// </summary>
public sealed class LocalFileStorageService(
    IOptions<FileStorageSettings> settings) : IFileStorageService
{
    private FileStorageSettings Settings => settings.Value;

    /// <inheritdoc />
    public async Task<string> SaveAsync(Stream stream, string relativePath, CancellationToken cancellationToken = default)
    {
        string fullPath = GetFullPath(relativePath);
        EnsurePathIsWithinRoot(fullPath);
        string? directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fileStream, cancellationToken);

        return relativePath;
    }

    /// <inheritdoc />
    public Task DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        string fullPath = GetFullPath(relativePath);
        EnsurePathIsWithinRoot(fullPath);

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        string fullPath = GetFullPath(relativePath);
        EnsurePathIsWithinRoot(fullPath);
        return Task.FromResult(File.Exists(fullPath));
    }

    /// <inheritdoc />
    public Task MoveAsync(string sourceRelativePath, string destinationRelativePath, CancellationToken cancellationToken = default)
    {
        string sourceFullPath = GetFullPath(sourceRelativePath);
        string destFullPath = GetFullPath(destinationRelativePath);
        EnsurePathIsWithinRoot(sourceFullPath);
        EnsurePathIsWithinRoot(destFullPath);
        string? destDirectory = Path.GetDirectoryName(destFullPath);

        if (!string.IsNullOrEmpty(destDirectory))
            Directory.CreateDirectory(destDirectory);

        File.Move(sourceFullPath, destFullPath);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        string fullPath = GetFullPath(relativePath);
        EnsurePathIsWithinRoot(fullPath);
        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    /// <inheritdoc />
    public async Task<string> ComputeChecksumAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        string fullPath = GetFullPath(relativePath);
        EnsurePathIsWithinRoot(fullPath);

        await using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var sha256 = SHA256.Create();
        byte[] hash = await sha256.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToBase64String(hash);
    }

    private string GetFullPath(string relativePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(relativePath);
        string rootPath = Settings.RootPath;
        if (string.IsNullOrWhiteSpace(rootPath))
            throw new InvalidOperationException("FileStorageSettings.RootPath is not configured.");
        return Path.GetFullPath(Path.Combine(rootPath, relativePath));
    }

    private void EnsurePathIsWithinRoot(string fullPath)
    {
        string rootPath = Path.GetFullPath(Settings.RootPath);
        if (!rootPath.EndsWith(Path.DirectorySeparatorChar))
            rootPath += Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException(
                $"Stien '{fullPath}' er utenfor lagringsområdet '{rootPath}'.");
    }
}
