# Feature: Budgeting

## Overview

The Budgeting module enables the business to create annual budgets broken down by account and month, compare actuals against budget in real time, project year-end outcomes based on year-to-date actuals, and copy prior-year budgets as a starting baseline.

## Goals

- Establish financial targets and track performance against them throughout the year.
- Identify overspending or underperformance early through variance analysis.
- Simplify annual budget preparation through copy-forward functionality.

## Requirements

| ID | Requirement | Priority |
|---|---|---|
| FR-BU-01 | Annual budget creation by account and month; amounts entered as monthly figures | Must Have |
| FR-BU-02 | Budget vs. actual variance report in absolute (CHF) and percentage terms for any period | Must Have |
| FR-BU-03 | Year-end forecast: actuals to date + remaining monthly budget amounts projected forward | Should Have |
| FR-BU-04 | Copy a prior year's budget as the starting point for a new budget year | Nice to Have |

## Key Domain Rules

- A budget is scoped to a fiscal year (configurable start month) and an account.
- Monthly budget amounts are stored individually to support uneven distributions.
- Variance = Actual − Budget; a positive variance on expense accounts is unfavourable (shown in red).
- The year-end forecast for month M of N: `Forecast = Actuals (months 1..M) + Budget (months M+1..N)`.
- Budget figures do not affect the General Ledger; they are a parallel planning layer only.
- Multiple budget versions per year are not supported in v1.0.

## Data Entities

- `Budget` — BudgetId, FiscalYear, Name, CreatedBy, CreatedAt
- `BudgetLine` — BudgetLineId, BudgetId, AccountId, Month (1–12), Amount

## Technical References

- SRS §3.9 (Budgeting Module), §3.2 FR-PL-04 (Budget vs. Actual on P&L)
- SDD §3.2 (Component Inventory — `Reports.razor`), §6.2 (Repository Pattern)
