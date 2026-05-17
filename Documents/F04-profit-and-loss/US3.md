# US3: P&L Line Item Drill-Down

**As an** accountant or business owner,
**I want** to click any line on the P&L and see the underlying journal entries that make up that figure,
**So that** I can verify the source of any revenue or expense amount without leaving the report.

## Acceptance Criteria

- [ ] Every account line on the P&L report is a clickable link (cursor: pointer, underline on hover).
- [ ] Clicking a line opens a slide-in panel or modal showing the filtered journal lines for that account within the report's date range.
- [ ] The drill-down panel shows columns: date, journal reference, description, amount, and memo.
- [ ] The total of the drill-down lines matches the P&L line amount exactly.
- [ ] The user can click a journal reference in the drill-down to navigate to the full journal entry in the General Ledger.
- [ ] The drill-down panel is closable via a close button or by pressing Escape.
- [ ] Subtotal rows (e.g. Gross Profit) are not clickable; only individual account rows trigger drill-down.

## Priority
Must Have

## Related Requirements
FR-PL-03
