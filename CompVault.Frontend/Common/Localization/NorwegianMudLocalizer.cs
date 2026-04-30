using Microsoft.Extensions.Localization;

using MudBlazor;
namespace CompVault.Frontend.Common.Localization;

/// <summary>
/// Klasse for å oversette MudGrid til norsk
/// </summary>
public class NorwegianMudLocalizer : MudLocalizer
{
    private readonly Dictionary<string, string> _translations = new()
    {
        // NavGroup
        { "MudNavGroup_ToggleExpand", "Utvid/skjul" },

        // Sortering
        { "MudDataGrid_Sort", "Sorter" },
        { "MudDataGrid_Unsort", "Fjern sortering" },
        { "MudDataGrid_SortedAscending", "Sortert stigende" },
        { "MudDataGrid_SortedDescending", "Sortert synkende" },

        // Kolonnevalg
        { "MudDataGrid_ShowColumnOptions", "Kolonnevalg" },
        { "MudDataGrid_HideColumn", "Skjul kolonne" },
        { "MudDataGrid_ShowColumns", "Vis kolonner" },

        // Filter
        { "MudDataGrid_FilterValue", "Filtrer etter verdi" },
        { "MudDataGrid_ClearFilter", "Fjern filter" },
        { "MudDataGrid_Contains", "Inneholder" },
        { "MudDataGrid_NotContains", "Inneholder ikke" },
        { "MudDataGrid_Equals", "Er lik" },
        { "MudDataGrid_NotEquals", "Er ikke lik" },
        { "MudDataGrid_StartsWith", "Starter med" },
        { "MudDataGrid_EndsWith", "Slutter med" },
        { "MudDataGrid_IsEmpty", "Er tom" },
        { "MudDataGrid_IsNotEmpty", "Er ikke tom" },

        // Paginering
        { "MudDataGrid_RowsPerPage", "Rader per side" },
        { "MudDataGridPager_RowsPerPage", "Rader per side" },
        { "MudDataGridPager_InfoFormat", "{0}-{1} av {2}" },

        // Valg og gruppering
        { "MudDataGrid_SelectAll", "Velg alle" },
        { "MudDataGrid_Group", "Grupper" },
        { "MudDataGrid_Save", "Lagre" },
        { "MudDataGrid_Cancel", "Avbryt" },
        { "MudDataGrid_True", "Sant" },
        { "MudDataGrid_False", "Usant" },
        { "MudDataGrid_RefreshData", "Oppdater" },
        { "MudDataGrid_CollapseAllGroups", "Skjul alle grupper" },
        { "MudDataGrid_ExpandAllGroups", "Vis alle grupper" },
    };

    public override LocalizedString this[string key]
    {
        get
        {
            if (_translations.TryGetValue(key, out string? value))
                return new LocalizedString(key, value);

            return new LocalizedString(key, key, resourceNotFound: true);
        }
    }
}