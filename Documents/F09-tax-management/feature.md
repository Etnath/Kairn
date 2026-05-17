# Feature: Tax Management

## Overview

The Tax Management module handles VAT/TVA configuration, automated VAT return report generation, tax period locking, and full data export for the external accountant. It is designed for Swiss VAT requirements (8.1% standard, 2.6% reduced, 0% exempt) but supports configurable rates for other jurisdictions.

## Goals

- Automate VAT computation so the business owner does not need to manually calculate tax liabilities.
- Generate ready-to-file VAT return reports for each tax period.
- Prevent retroactive changes to filed periods through period locking.
- Make the annual accountant handover a one-click operation.

## Requirements

| ID | Requirement | Priority |
|---|---|---|
| FR-TX-01 | VAT rate configuration by category: name, rate %, valid from/to dates, default flag | Must Have |
| FR-TX-02 | VAT return report for any period: input tax (deductible), output tax (collectible), net payable | Must Have |
| FR-TX-03 | Tax period locking: locked periods reject any new or modified transactions | Must Have |
| FR-TX-04 | Accountant export: journal entries, P&L, and Balance Sheet in Excel and PDF for any period | Must Have |

## Key Domain Rules

- VAT rates are date-ranged; the rate in effect on the transaction date is applied automatically.
- Output tax = VAT charged on sales (AR invoices); Input tax = VAT paid on purchases (AP bills).
- Net VAT payable = Output tax − Input tax; a negative value represents a VAT refund due.
- Period locking is irreversible without an Admin override; an unlock event is recorded in the audit log.
- The accountant export bundles: Trial Balance, P&L, Balance Sheet, full journal entry listing, and VAT summary into a single ZIP containing Excel and PDF files.

## Data Entities

- `TaxRate` — TaxRateId, Name, Rate, Category, IsDefault, ValidFrom, ValidTo
- `TaxPeriod` — TaxPeriodId, Name, StartDate, EndDate, IsLocked, LockedBy, LockedAt

## Technical References

- SRS §3.8 (Tax Management Module), §2.5 (Swiss OR/CO standards)
- SDD §3.2 (`Settings.razor`), §5.5 (PDF Generation — QuestPDF), §6.1 (Data Layer)
