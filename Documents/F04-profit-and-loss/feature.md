# Feature: Profit & Loss

## Overview

The Profit & Loss module provides real-time income statement analysis. Users can generate a P&L statement for any custom date range, compare periods side-by-side, drill down from summary lines to underlying journal entries, overlay budget figures, and export results.

## Goals

- Give business owners and accountants instant visibility into profitability for any time period.
- Support period-over-period comparison to identify trends.
- Link high-level P&L figures to source transactions for full transparency.
- Allow export in PDF and CSV for external review.

## Requirements

| ID | Requirement | Priority |
|---|---|---|
| FR-PL-01 | P&L Statement for any user-defined period showing Revenue, COGS, Gross Profit, Operating Expenses, EBITDA, and Net Income | Must Have |
| FR-PL-02 | Side-by-side period comparison (e.g. this month vs. last month, this year vs. last year) | Must Have |
| FR-PL-03 | Clicking any P&L line item drills down to the underlying transactions | Must Have |
| FR-PL-04 | Budget vs. Actual overlay with variance in absolute and percentage terms | Should Have |
| FR-PL-05 | Export as PDF and CSV | Must Have |

## Key Behaviour

- The report is computed from the General Ledger; no denormalised P&L storage.
- Account groupings (Revenue, COGS, Operating Expenses, etc.) follow the Chart of Accounts structure.
- EBITDA is derived: Net Income + Interest + Tax + Depreciation + Amortisation.
- Date range presets: This Month, Last Month, This Quarter, YTD, Last Year, Custom.
- Drill-down opens a filtered view of journal lines for the selected account and period.
- Negative variances (actual worse than budget) are displayed in red.

## Technical References

- SRS §3.2 (Profit & Loss Module)
- SDD §3.2 (`ProfitLossReport.razor`, `Reports.razor`), §4.1 (Component Anatomy)
