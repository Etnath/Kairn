# US1: Full English and French Localisation

**As a** business owner or accountant,
**I want** every screen in the application to be available in both English and French, and to be able to switch language at any time,
**So that** francophone users can use the application comfortably in their preferred language from day one.

## Acceptance Criteria

### Language Switching
- [ ] The `LanguageSwitcher` component (in the top app bar) displays the active language as a labelled button: "EN" or "FR".
- [ ] Clicking the button switches to the other language, sets the `lang` cookie (path `/`, max-age 30 days), and performs a full page reload to apply the new `CultureInfo`.
- [ ] After switching, all visible text, labels, validation messages, and tooltips render in the selected language.
- [ ] The language selection persists across sessions; a returning user sees the application in the language they last chose.
- [ ] If no `lang` cookie is present, the browser's `Accept-Language` header is used as the fallback; if neither matches a supported culture, English is used.

### Shell and Navigation
- [ ] All nav menu entries are translated:

  | Key | English | French |
  |---|---|---|
  | Nav.Dashboard | Dashboard | Tableau de bord |
  | Nav.GeneralLedger | General Ledger | Grand livre |
  | Nav.Invoicing | Invoicing | Facturation |
  | Nav.Bills | Bills | Factures fournisseurs |
  | Nav.Reports | Reports | Rapports |
  | Nav.MarginAnalysis | Margin Analysis | Analyse des marges |
  | Nav.Tax | Tax | TVA & Fiscalité |
  | Nav.Budgets | Budgets | Budgets |
  | Nav.Settings | Settings | Paramètres |

- [ ] The app bar title "Kairn" remains unchanged in both languages.
- [ ] The login page labels, placeholders, button text, and error messages are translated in full.

### Module Pages and Components
- [ ] Every module page title, section heading, column header, button label, form field label, placeholder text, validation message, and snackbar message is defined in a `.resx` resource pair (`.en.resx` and `.fr.resx`).
- [ ] No hard-coded English strings remain in any `.razor` or `.cs` file in `Kairn.Blazor`; all user-facing text is accessed via `IStringLocalizer<T>`.
- [ ] Status badge labels are translated (e.g. "Draft" → "Brouillon", "Paid" → "Payée", "Overdue" → "En retard").
- [ ] Report titles and column headers are translated (e.g. "Profit & Loss" → "Compte de résultat", "Balance Sheet" → "Bilan", "Trial Balance" → "Balance de vérification").
- [ ] Error and confirmation dialog messages are translated.
- [ ] Tooltip and contextual help strings are translated.

### Number, Date, and Currency Formatting
- [ ] In English (`en`): dates display as `MM/dd/yyyy`; decimal separator is `.`; thousands separator is `,` (e.g. `1,234.56`).
- [ ] In French (`fr`): dates display as `dd/MM/yyyy`; decimal separator is `,`; thousands separator is ` ` (non-breaking space, e.g. `1 234,56`).
- [ ] Currency amounts always include the ISO code or symbol (CHF, €, $, £); the position of the symbol follows locale convention.
- [ ] The `CurrencyInput` component respects the active locale for decimal entry and display.
- [ ] The `DateRangePicker` preset button labels are translated (e.g. "This Month" → "Ce mois-ci", "Last Month" → "Mois dernier", "YTD" → "Depuis le début de l'année", "Last Year" → "Année dernière").

### Export Documents
- [ ] PDF and CSV exports (P&L, Balance Sheet, Trial Balance, etc.) are generated in the language active at the time of export.
- [ ] Column headers, section labels, and footers in exported PDFs are in the selected language.
- [ ] The export file name uses locale-neutral date segments (ISO 8601: `YYYY-MM-DD`) regardless of locale.

### Testing
- [ ] A localisation test suite in `Kairn.Tests` covers: switching culture to `fr` returns French strings for all localiser keys in every resource file; no missing-key fallbacks (marked `???key???` in MudBlazor) are present in either language.
- [ ] A Playwright E2E test switches the UI to French and asserts that the nav menu label "Grand livre" is visible and the login form renders in French.

## Notes

- Use the `dotnet` embedded resource approach: `.resx` files are compiled into satellite assemblies per culture (`fr/Kairn.Blazor.resources.dll`).
- `fr` culture uses `fr-CH` number formatting (Swiss French) for number display to match the CHF-primary audience.
- MudBlazor's own component strings (e.g. date picker day names, pagination labels) must be overridden via `MudBlazor.Resources` or a custom `MudLocalizer` implementation for the `fr` culture.
- All future stories must add resource keys to both `.en.resx` and `.fr.resx` files as part of the story's definition of done.

## Priority
Must Have

## Related Requirements
SRS NFR-U-03
SDD §7.1 (Layout), §7.3 (Custom Components)
