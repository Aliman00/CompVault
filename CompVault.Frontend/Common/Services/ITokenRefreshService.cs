using CompVault.Frontend.Common.Http.Models;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Common.Services;

public interface ITokenRefreshService
{
    Task<Result<RefreshRecord>> RefreshPairAsync(string userId, HttpContext httpContext,
        CancellationToken ct = default);

    void ApplyTokenPair(HttpContext httpContext, RefreshRecord refreshRecord);
}