# US3: Lock Tax Periods

**As an** administrator,
**I want** to lock a tax period after filing so that no transactions can be added, edited, or deleted within that period,
**So that** filed tax figures remain accurate and tamper-proof.

## Acceptance Criteria

- [ ] A "Tax Periods" section under Tax Management lists defined periods (quarterly by default, configurable).
- [ ] An Admin can lock a tax period by clicking "Lock Period" and confirming the action in a dialog that warns: "Locking this period will prevent all changes to transactions dated [start] to [end]. This action requires Admin override to reverse."
- [ ] Once locked, any attempt to create, edit, or delete a journal entry, invoice, bill, or payment within the period's date range returns an error: "This transaction falls within a locked tax period."
- [ ] A locked period displays a padlock icon in the period list with the locking user and timestamp.
- [ ] An Admin can unlock a period via "Override Unlock"; this action requires a mandatory reason field and is recorded in the audit log with an elevated warning log level.
- [ ] The VAT Return report clearly marks which periods are locked vs. open.
- [ ] Locking and unlocking events are always audited, even if audit trail for other actions is disabled.

## Priority
Must Have

## Related Requirements
FR-TX-03, FR-GL-07, NFR-S-04
