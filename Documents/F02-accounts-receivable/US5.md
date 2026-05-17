# US5: Invoice Status Lifecycle and Overdue Transitions

**As a** business owner,
**I want** overdue invoices to be flagged automatically when their due date passes,
**So that** I never need to manually monitor unpaid invoices to identify collection actions.

## Acceptance Criteria

- [ ] A background job runs nightly and transitions any **Sent** or **Partially Paid** invoice past its due date to **Overdue** status.
- [ ] Overdue invoice rows are tinted Signal 50 `#FAECE8` on the invoice list and on the customer detail page.
- [ ] The dashboard alerts panel shows the count and total value of overdue invoices with a link to the AR aging report.
- [ ] An invoice can be manually voided by an Admin or Bookkeeper; voiding posts a reversing journal entry and sets status to **Void**.
- [ ] A void invoice cannot be paid or edited; it is visible in the invoice list with a "Void" badge.
- [ ] Void and Paid invoices are hidden from the default invoice list filter; a "Show all statuses" toggle reveals them.
- [ ] The `StatusBadge` component renders each status using the exact token pairings from `Kairn_ColorSystem.md`:

  | Status | Background | Text |
  |---|---|---|
  | Draft | Stone 200 `#D6D3CA` | Stone 700 `#4A4843` |
  | Sent | Slate 50 `#EAF0F6` | Slate 700 `#2A4F6E` |
  | Partially Paid | Summit 50 `#FBF2E0` | Summit 700 `#8F6008` |
  | Paid | Lichen 50 `#E8F4ED` | Lichen 700 `#1F6040` |
  | Overdue | Signal 50 `#FAECE8` | Signal 700 `#7E2A14` |
  | Void | Stone 200 `#D6D3CA` | Stone 500 `#8C8980` |

## Priority
Must Have

## Related Requirements
FR-AR-03, FR-DB-03
