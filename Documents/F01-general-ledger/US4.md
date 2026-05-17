# US4: Bank Account Reconciliation

**As a** bookkeeper or business owner,
**I want** to import a bank statement and match its transactions to journal entries in the ledger,
**So that** I can confirm the ledger matches the bank and identify missing or erroneous entries.

## Acceptance Criteria

- [ ] The General Ledger module includes a "Reconcile" page accessible from the nav or ledger toolbar.
- [ ] The user selects a bank account (from the COA) and a statement date range to begin reconciliation.
- [ ] The user uploads a bank statement file in OFX, QFX, or CSV format; the system parses it and displays the imported transactions.
- [ ] The page shows two panels side-by-side: bank statement transactions (left) and unreconciled ledger entries for the selected account (right).
- [ ] The user can match a bank statement line to one or more ledger lines by selecting rows in both panels and clicking "Match".
- [ ] Matched pairs are highlighted and moved to a "Matched" section; the running unmatched balance updates in real time.
- [ ] A "Create Entry" shortcut opens the `JournalEntryDialog` pre-filled with the bank transaction details, allowing the user to record missing entries without leaving the reconciliation view.
- [ ] When all bank statement transactions are matched and the unmatched difference is CHF 0.00, the user can click "Complete Reconciliation" to mark the period as reconciled.
- [ ] Completed reconciliations are saved with the date, account, and matched pair count; they can be reviewed but not re-opened.
- [ ] Unmatched bank transactions are listed in a summary so the user knows what still needs investigation.

## Notes

- CSV import uses configurable column mapping (date column, description column, amount column, debit/credit column).
- OFX/QFX parsing uses a server-side library; no external service call is required.

## Priority
Must Have

## Related Requirements
FR-GL-04, §5.3 (Bank Statement Import)
