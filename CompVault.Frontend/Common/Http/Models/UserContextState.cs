namespace CompVault.Frontend.Common.Http.Models;

public record UserContextState(
    string AuthenticationType,
    List<ClaimData> Claims,
    string RefreshToken);