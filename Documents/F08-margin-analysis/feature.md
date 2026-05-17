# Feature: Margin Analysis

## Overview

The Margin Analysis module allows the business to track revenue and costs broken down by product or service category, compute gross and net margins, visualise margin trends over time, and alert the user when profitability falls below acceptable thresholds.

## Goals

- Provide visibility into which products or services are most (and least) profitable.
- Enable data-driven pricing and cost management decisions.
- Surface margin deterioration proactively through configurable alerts.

## Requirements

| ID | Requirement | Priority |
|---|---|---|
| FR-MA-01 | Define product/service categories and tag revenue and COGS accounts to each | Must Have |
| FR-MA-02 | Gross Margin report: Revenue − COGS per category for any selected period | Must Have |
| FR-MA-03 | Net Margin report: Gross Margin minus allocated operating expenses per category | Should Have |
| FR-MA-04 | Margin trend line chart per category over time (monthly) | Should Have |
| FR-MA-05 | Alert when gross margin on any category falls below a user-configurable threshold | Nice to Have |

## Key Domain Rules

- `Gross Margin % = (Revenue − COGS) / Revenue × 100`
- `Net Margin % = (Revenue − COGS − Allocated OpEx) / Revenue × 100`
- Operating expense allocation is optional; if not configured, Net Margin equals Gross Margin.
- A category with zero revenue in the selected period is shown with a gross margin of 0% (not omitted).
- Margin thresholds are stored per category in system settings; breach triggers an in-app alert and optionally a dashboard notification.

## Data Entities

- `ProductLine` — ProductLineId, Name, Description, RevenueAccountId, CogsAccountId, MarginAlertThreshold
- Margin data is computed at query time from `JournalLine` records; no separate margin table.

## Technical References

- SRS §3.6 (Margin Analysis Module)
- SDD §3.2 (`MarginAnalysis.razor`, `MarginChart`), §5.3 (Real-Time Notifications via SignalR)
