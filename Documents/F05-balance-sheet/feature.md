# Feature: Balance Sheet

## Overview

The Balance Sheet module provides a snapshot of the business's financial position (Assets = Liabilities + Equity) at any point in time. It includes a fixed asset register with automated depreciation, equity tracking, and comparative reporting.

## Goals

- Give stakeholders a complete, auditable picture of the business's net worth at any date.
- Automate fixed-asset depreciation to eliminate manual monthly journal entries.
- Track owner equity movements including capital contributions, withdrawals, and retained earnings.

## Requirements

| ID | Requirement | Priority |
|---|---|---|
| FR-BS-01 | Balance Sheet report (Assets = Liabilities + Equity) for any selected date, with current and non-current categories | Must Have |
| FR-BS-02 | Fixed asset register: purchase date, value, depreciation method (straight-line or declining balance), net book value | Must Have |
| FR-BS-03 | Automated monthly depreciation journal entries calculated from the asset register | Must Have |
| FR-BS-04 | Equity tracking: capital contributions, withdrawals, retained earnings movements | Must Have |
| FR-BS-05 | Comparative Balance Sheet: two dates side-by-side with movement column | Should Have |

## Key Domain Rules

- The balance sheet equation `Assets = Liabilities + Equity` must always hold after every committed transaction.
- Straight-line depreciation: `Monthly Charge = (Purchase Value − Residual Value) / (Useful Life in Months)`.
- Declining balance depreciation: `Monthly Charge = Net Book Value × (Rate / 12)`.
- Automated depreciation entries are posted at period-end by a scheduled background job and cannot be manually overridden without reversal.
- Assets are deactivated (not deleted) when fully depreciated or disposed of.

## Data Entities

- `FixedAsset` — AssetId, Name, Category, PurchaseDate, PurchaseValue, ResidualValue, DepreciationMethod, UsefulLifeYears, AssetAccountId, AccumulatedDepreciationAccountId

## Technical References

- SRS §3.3 (Balance Sheet Module)
- SDD §3.2 (`BalanceSheetReport.razor`), §6.1 (Data Layer)
