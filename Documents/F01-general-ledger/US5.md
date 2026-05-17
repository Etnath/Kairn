# US5: Generate Trial Balance

**As an** accountant or business owner,
**I want** to generate a Trial Balance report as of any date,
**So that** I can verify that total debits equal total credits across all accounts and identify any imbalances.

## Acceptance Criteria

- [ ] A "Trial Balance" option is available under Reports.
- [ ] The user selects an "as of" date; the report computes opening balances, period movements, and closing balances for all active accounts.
- [ ] The report is displayed in a table with columns: Account Code, Account Name, Debit Balance, Credit Balance.
- [ ] The footer row shows total debits and total credits; both must be equal for the ledger to be in balance.
- [ ] If totals are unequal, a red warning banner is shown: "Ledger is out of balance — please investigate."
- [ ] Accounts with zero balance are included by default; a "Hide zero balances" toggle collapses them.
- [ ] The report can be exported as PDF and CSV.
- [ ] Generating the report for up to 100,000 journal lines completes in under 3 seconds.

## Notes

- The Trial Balance is computed from `JournalLine` records; no materialised view is used in v1.0.
- A properly functioning double-entry system should never produce an unbalanced trial balance; the warning exists as a safeguard for data migration edge cases.

## Priority
Must Have

## Related Requirements
FR-GL-05, NFR-P-01, §5.5 (PDF Generation)
