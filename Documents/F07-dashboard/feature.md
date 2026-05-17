# Feature: Dashboard

## Overview

The Dashboard is the default landing page for all user roles. It presents live KPI cards, revenue and expense charts, a cash position chart, and an alerts panel surfacing overdue invoices, upcoming payables, and low-cash warnings. An optional customisable widget layout is planned for a later iteration.

## Goals

- Give business owners an at-a-glance health check of the business in under 5 seconds.
- Surface actionable alerts so nothing falls through the cracks.
- Provide visual trend data without requiring navigation into individual modules.

## Requirements

| ID | Requirement | Priority |
|---|---|---|
| FR-DB-01 | KPI cards: Monthly Revenue, Monthly Expenses, Net Profit, Outstanding AR, Outstanding AP, Cash Balance | Must Have |
| FR-DB-02 | Charts: revenue vs. expenses bar chart (monthly, last 12 months), P&L trend line chart, cash position chart | Must Have |
| FR-DB-03 | Alerts panel: overdue invoices, upcoming payables (next 7 days), low cash balance (below configurable threshold) | Must Have |
| FR-DB-04 | Customisable layout: users can choose which KPI widgets appear | Nice to Have |

## Key Behaviour

- Dashboard data is loaded asynchronously on first render; `MudSkeleton` loaders are shown during fetch.
- KPI cards display the current-month value, a delta percentage vs. the prior month, and a trend arrow coloured green (positive) or red (negative).
- Revenue vs. expenses bar chart groups by calendar month; clicking a bar navigates to the P&L report for that month.
- Alerts panel auto-refreshes via Fluxor dispatch every 5 minutes while the tab is active.
- Cash balance threshold is configurable in Settings; default is CHF 5,000.
- Roles: all authenticated roles can view the dashboard; KPI values and alerts respect the user's data scope.

## Technical References

- SRS §3.7 (Dashboard Module)
- SDD §3.2 (`Dashboard.razor`, `KpiCard.razor`, `DashboardStore.cs`), §4.2 (Fluxor State Management), §7.4 (Progressive Loading)
