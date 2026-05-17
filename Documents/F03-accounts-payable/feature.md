# Feature: Accounts Payable

## Overview

The Accounts Payable module manages vendor relationships and the full purchase-to-pay cycle: vendor records, bill entry, payment scheduling, aging analysis, and expense report submission. All AP events post automatically to the General Ledger.

## Goals

- Give the business a clear view of what is owed to vendors and when payments are due.
- Prevent missed payments through upcoming-payable scheduling.
- Streamline expense reimbursement with receipt-attached expense reports.

## Requirements

| ID | Requirement | Priority |
|---|---|---|
| FR-AP-01 | Vendor database: contact details, payment terms, IBAN/bank details, default expense account | Must Have |
| FR-AP-02 | Bill entry: vendor, line items, due date, expense category | Must Have |
| FR-AP-03 | Payment scheduling: view upcoming payables and mark bills as scheduled or paid | Must Have |
| FR-AP-04 | Bill approval workflow: bills above a configurable threshold require approval before payment | Nice to Have |
| FR-AP-05 | AP aging report: amounts owed grouped by due date (current, 30/60/90/90+ days) | Must Have |
| FR-AP-06 | Expense reports: submit multi-line expense claims with receipt attachments | Should Have |

## Key Domain Rules

- A bill transitions to **Overdue** automatically when its due date passes and the balance is unpaid.
- Bills must be categorised to an expense account from the Chart of Accounts.
- The approval threshold (FR-AP-04) is configurable per role in system settings.
- Marking a bill as paid posts a credit to the configured bank/cash account and a debit to AP.
- Expense report lines are itemised; the total posts to the appropriate expense accounts after approval.

## Data Entities

- `Vendor` — VendorId, Name, ContactEmail, IBAN, PaymentTermsDays, DefaultExpenseAccountId
- `Bill` — BillId, VendorId, Date, DueDate, Status, Lines[], TaxAmount, TotalAmount
- `BillLine` — BillLineId, BillId, Description, Quantity, UnitPrice, AccountId, TaxRateId
- `ExpenseReport` — ExpenseReportId, SubmittedBy, Date, Status, Lines[], TotalAmount

## Technical References

- SRS §3.5 (Accounts Payable Module)
- SDD §3.2 (Component Inventory), §6.2 (Repository Pattern)
