# US7: Multi-Currency Journal Entries

**As a** business owner operating across multiple currencies,
**I want** to enter journal lines in CHF, EUR, USD, or GBP with automatic conversion to the base currency,
**So that** transactions in foreign currencies are correctly reflected in the base-currency ledger and reports.

## Acceptance Criteria

- [ ] Each journal line has a currency selector (CHF, EUR, USD, GBP); it defaults to the account's configured currency.
- [ ] When a non-base currency is selected, the system fetches the exchange rate for the transaction date from the configured API (Frankfurter/ECB) and displays it in a read-only field.
- [ ] The user can manually override the exchange rate; the override is recorded in the journal line alongside the system rate.
- [ ] The base-currency equivalent (`Amount × ExchangeRate`) is displayed next to the foreign-currency amount in the entry form.
- [ ] The balance validation uses base-currency equivalents: `SUM(Debit × Rate) = SUM(Credit × Rate)`.
- [ ] If the exchange rate API is unavailable, the last cached rate for that currency pair is used and a warning banner is shown: "Using cached exchange rate from [date]."
- [ ] Exchange rates are refreshed daily at midnight and stored in a local `ExchangeRate` table (`CurrencyPair`, `Date`, `Rate`).
- [ ] Reports (P&L, Balance Sheet, Trial Balance) present all amounts in the base currency (CHF), with foreign-currency details available in the drill-down view.

## Notes

- Base currency is CHF by default; it is configurable in Settings for non-Swiss entities.
- Currency pairs supported: CHF/EUR, CHF/USD, CHF/GBP (and their inverses).

## Priority
Should Have

## Related Requirements
FR-GL-08, §5.2 (Currency Exchange Rate API)
