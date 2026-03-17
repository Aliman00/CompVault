namespace CompVault.Shared.Result;

/// <summary>
/// Et custom problem details objekt som viser appens egen feilmelding, statuskode og message
/// </summary>
public class ProblemDetail
{
    public int Status { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
