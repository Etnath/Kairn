# US4: Equity Tracking

**As a** business owner,
**I want** the system to track movements in owner equity including capital contributions, withdrawals, and retained earnings,
**So that** the equity section of the Balance Sheet accurately reflects the owner's stake in the business at all times.

## Acceptance Criteria

- [ ] The Chart of Accounts seed includes equity accounts: Share Capital, Owner Drawings, Retained Earnings, and Current Year Earnings.
- [ ] Capital contributions are recorded as manual journal entries: debit Bank/Cash account, credit Share Capital.
- [ ] Owner withdrawals are recorded as manual journal entries: debit Owner Drawings account, credit Bank/Cash account.
- [ ] At fiscal year-end, a "Close Year" action is available to Admin, which posts a closing entry: debit/credit Current Year Earnings and transfer the balance to Retained Earnings.
- [ ] The "Close Year" action is irreversible; it locks the fiscal year and requires confirmation before proceeding.
- [ ] The equity section of the Balance Sheet shows the balance of each equity account clearly, with the net of Retained Earnings + Current Year Earnings labelled "Total Equity".
- [ ] Equity account balances and movements are included in the drill-down from the Balance Sheet.

## Priority
Must Have

## Related Requirements
FR-BS-04
