# US3: Net Margin Report

**As an** accountant or business owner,
**I want** to see the net margin per product line after allocating a share of operating expenses,
**So that** I can understand the true profitability of each line of business including overhead.

## Acceptance Criteria

- [ ] A "Net Margin" tab or toggle is available on the Margin Analysis page alongside the Gross Margin view.
- [ ] An Admin can configure operating expense allocation rules per product line: percentage of total OpEx to allocate (e.g. line A gets 60%, line B gets 40%).
- [ ] If no allocation rules are configured, a message explains that net margin equals gross margin when no OpEx is allocated.
- [ ] The net margin report adds columns: allocated operating expenses, net profit, and net margin %.
- [ ] Net Profit = Gross Profit − Allocated OpEx; Net Margin % = Net Profit / Revenue × 100.
- [ ] Negative net margin is shown in red.
- [ ] Export buttons for PDF and CSV include the net margin columns.

## Priority
Should Have

## Related Requirements
FR-MA-03
