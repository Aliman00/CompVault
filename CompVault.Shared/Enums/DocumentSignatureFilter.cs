namespace CompVault.Shared.Enums;

/// <summary>
/// Filter for om vi henter alle, singerte eller ikke-signerte dokumenter
/// </summary>
public enum DocumentSignatureFilter
{   
    All,
    Signed,
    Pending,
}