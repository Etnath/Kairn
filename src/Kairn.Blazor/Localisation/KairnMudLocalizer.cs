using Microsoft.Extensions.Localization;
using MudBlazor;

namespace Kairn.Blazor.Localisation;

public sealed class KairnMudLocalizer : MudLocalizer
{
    private static readonly Dictionary<string, string> _fr = new()
    {
        // DataGrid — filter operators
        ["MudDataGrid.FilterValue"]       = "Valeur du filtre",
        ["MudDataGrid.operator"]          = "Opérateur",
        ["MudDataGrid.contains"]          = "Contient",
        ["MudDataGrid.not_contains"]      = "Ne contient pas",
        ["MudDataGrid.equals"]            = "Égal à",
        ["MudDataGrid.not_equals"]        = "Différent de",
        ["MudDataGrid.starts_with"]       = "Commence par",
        ["MudDataGrid.ends_with"]         = "Se termine par",
        ["MudDataGrid.is_empty"]          = "Est vide",
        ["MudDataGrid.is_not_empty"]      = "N'est pas vide",
        ["MudDataGrid.is"]                = "Est",
        ["MudDataGrid.is_not"]            = "N'est pas",
        ["MudDataGrid.is_before"]         = "Est avant",
        ["MudDataGrid.is_after"]          = "Est après",
        ["MudDataGrid.is_on_or_before"]   = "Le ou avant",
        ["MudDataGrid.is_on_or_after"]    = "Le ou après",
        ["MudDataGrid.True"]              = "Vrai",
        ["MudDataGrid.False"]             = "Faux",
        ["MudDataGrid.CollapseAllGroups"] = "Réduire tout",
        ["MudDataGrid.ExpandAllGroups"]   = "Développer tout",
        ["MudDataGrid.AddFilter"]         = "Ajouter un filtre",
        ["MudDataGrid.apply"]             = "Appliquer",
        ["MudDataGrid.Group"]             = "Grouper",
        ["MudDataGrid.Ungroup"]           = "Dissocier",
        ["MudDataGrid.HideColumn"]        = "Masquer la colonne",
        ["MudDataGrid.ShowColumns"]       = "Afficher les colonnes",
        ["MudDataGrid.Filter"]            = "Filtrer",
        ["MudDataGrid.Save"]              = "Enregistrer",
        ["MudDataGrid.Cancel"]            = "Annuler",
        ["MudDataGrid.Dense"]             = "Compact",
        ["MudDataGrid.unsorted"]          = "Non trié",
        ["MudDataGrid.sortAscending"]     = "Trier par ordre croissant",
        ["MudDataGrid.sortDescending"]    = "Trier par ordre décroissant",

        // Table pager
        ["MudTablePager.RowsPerPage"]     = "Lignes par page :",
        ["MudTablePager.of"]              = "sur",
        ["MudTablePager.All"]             = "Tout",

        // Pagination
        ["MudPagination.First"]           = "Première page",
        ["MudPagination.Last"]            = "Dernière page",
        ["MudPagination.Previous"]        = "Page précédente",
        ["MudPagination.Next"]            = "Page suivante",

        // Date / time pickers
        ["MudDateRangePicker.StartDate"]  = "Date de début",
        ["MudDateRangePicker.EndDate"]    = "Date de fin",
        ["MudDatePicker.KeyboardDateInputAria"] = "Saisie de date au clavier",
        ["MudTimePicker.OpenTimePickerAria"]    = "Ouvrir le sélecteur d'heure",
        ["MudTimePicker.CloseTimePickerAria"]   = "Fermer le sélecteur d'heure",

        // File upload
        ["MudFileUpload.DefaultDragAndDropZoneText"] = "Faites glisser les fichiers ici",

        // Autocomplete / select
        ["MudAutocomplete.NoResultsText"] = "Aucun résultat",
        ["MudSelect.MultiSelectAllText"]  = "Tout sélectionner",
        ["MudSelect.MultiSelectNoneText"] = "Tout désélectionner",
    };

    public override LocalizedString this[string key]
    {
        get
        {
            var lang = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (lang == "fr" && _fr.TryGetValue(key, out var value))
                return new LocalizedString(key, value);

            return new LocalizedString(key, key, resourceNotFound: true);
        }
    }
}
