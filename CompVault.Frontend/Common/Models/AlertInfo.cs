using MudBlazor;
namespace CompVault.Frontend.Common.Models;

/// <summary>
/// En enkel record for å sette melding og severity i flere komponenter og sider
/// </summary>
/// <param name="Message">Error/Suksess-meldingen som er synlig</param>
/// <param name="Severity">Hvilken farge banneren skal ha. Eks: Error eller Success </param>
public record AlertInfo(string Message, Severity Severity);