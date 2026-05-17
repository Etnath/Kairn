# US2: Create and Edit Journal Entries

**As a** bookkeeper or business owner,
**I want** to create manual journal entries with one or more debit and credit lines,
**So that** I can record any financial transaction in the system with the double-entry method enforced.

## Acceptance Criteria

- [ ] The General Ledger page displays a paginated, filterable table of journal entries (date, reference, description, total amount, created by).
- [ ] An "Add Entry" button opens a `JournalEntryDialog` modal with fields: date (required), reference (required, auto-generated if left blank), description (required), and a line-item grid.
- [ ] Each line item has: account (searchable COA dropdown), debit amount, credit amount, currency, and memo.
- [ ] The dialog displays a running balance indicator: `Debits − Credits`; the indicator is Lichen 500 `#3A9463` when zero (balanced) and Signal 500 `#C2492A` when non-zero (unbalanced).
- [ ] The Save button is disabled while the entry is unbalanced (debits ≠ credits).
- [ ] On submit, the domain layer validates balance to 4 decimal places; if invalid, the server returns an error displayed in a MudSnackbar.
- [ ] A successfully saved entry closes the dialog, appends the new row to the ledger table, and shows a success snackbar.
- [ ] An existing entry can be edited by clicking it, provided it is not locked.
- [ ] Editing a posted entry creates a new version (the original is preserved in the audit log); the UI reflects the latest version.
- [ ] A minimum of 2 lines (one debit, one credit) is required.
- [ ] Optional receipt attachment: PDF or image file up to 10 MB, stored server-side.
- [ ] All journal entry saves and edits are recorded in the audit log.

## Notes

- The auto-generated reference format is `JE-YYYYMMDD-NNN` where NNN is a zero-padded sequential counter for the day.
- Line-item amounts default to 0.00; the user enters either debit or credit, not both on the same line.

## Priority
Must Have

## Related Requirements
FR-GL-02, FR-GL-03, FR-GL-07
