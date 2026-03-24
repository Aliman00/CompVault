using CompVault.Backend.Common.Responses;
using CompVault.Shared.Result;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace CompVault.Backend.Common.Authorization;

/// <summary>
/// Håndterer authorization failures og returnerer ProblemDetail istedenfor tom body.
/// </summary>
public sealed class AuthorizationFailureHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Succeeded)
        {
            await next(context);
            return;
        }

        if (authorizeResult.Forbidden)
        {
            string message = GetForbiddenMessage(authorizeResult);
            ProblemDetail problem = ProblemDetailBuilder.Create(403, ErrorCode.Forbidden.ToString(), message);

            context.Response.StatusCode = problem.Status;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(problem);
            return;
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }

    private static string GetForbiddenMessage(PolicyAuthorizationResult result)
    {
        if (result.AuthorizationFailure?.FailureReasons?.Any() == true)
        {
            IEnumerable<string> reasons = result.AuthorizationFailure.FailureReasons
                .Select(r => r.Message);
            return string.Join("; ", reasons);
        }

        return "Du har ikke den nødvendige rollen eller tillatelsen til å aksessere denne ressursen.";
    }
}
