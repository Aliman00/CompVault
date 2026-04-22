using CompVault.Frontend.Common.Constants;
namespace CompVault.Frontend.Common.Models;

/// <summary>
/// En gruppert mimetype. Eks: Dokumenter med mange MimeTypeOptions som PDF, Docs etc.
/// </summary>
/// <param name="GroupLabel">Gruppelabel med valg til brukeren</param>
/// <param name="Types">En liste med MimeTypeOption</param>
public record MimeTypeGroup(string GroupLabel, IReadOnlyList<MimeTypeOption> Types);