using System.Runtime.CompilerServices;
namespace CompVault.Frontend.Common.Http;

/// <summary>
/// Hjelpemetode som sikrer at parallele requester venter på samme Refresh Token-kall-
/// Brukes i CookieValidationEvents
/// </summary>
internal sealed class AsyncLazy<T>(Func<Task<T>> factory) : Lazy<Task<T>>(factory)
{
    public TaskAwaiter<T> GetAwaiter() => Value.GetAwaiter();
}