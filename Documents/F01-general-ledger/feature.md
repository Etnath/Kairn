# Feature: General Ledger

## Overview

The General Ledger is the core financial engine of Kairn. Every monetary transaction in the system is recorded as a double-entry journal entry. The module provides the Chart of Accounts, journal entry management, bank reconciliation, trial balance, recurring entries, audit trail, and multi-currency support.

## Goals

- Provide a fully functional double-entry bookkeeping system enforced at the domain layer.
- Allow users to manage accounts, record transactions, and reconcile bank statements.
- Support accountant-level reporting through the Trial Balance.
- Ensure complete immutability and traceability of all financial data via soft-delete and audit logging.

## Requirements

| ID | Requirement | Priority |
|---|---|---|
| FR-GL-01 | Configurable Chart of Accounts (Assets, Liabilities, Equity, Revenue, Expenses) with code, name, type, currency | Must Have |
| FR-GL-02 | Manual journal entry creation; debits must equal credits or entry is rejected | Must Have |
| FR-GL-03 | Unique reference number, date, description, optional PDF/image attachment per entry | Must Have |
| FR-GL-04 | Bank account reconciliation by matching transactions to imported bank statements (OFX, QFX, CSV) | Must Have |
| FR-GL-05 | Trial Balance report as of any selected date | Must Have |
| FR-GL-06 | Recurring journal entries with configurable frequency and end date | Should Have |
| FR-GL-07 | Full audit trail — soft-delete only; all create/modify/delete events logged with timestamp and user | Must Have |
| FR-GL-08 | Multi-currency journal entries with automatic base-currency conversion using exchange rate at transaction date | Should Have |

## Key Domain Rules

- Every journal entry must satisfy: `SUM(Debits) = SUM(Credits)` to 4 decimal places.
- Deletion is always soft (flag `IsDeleted = true`); hard deletes are never permitted.
- Locked entries (within a locked tax period) cannot be modified.
- Exchange rates are fetched from the configured currency API and cached daily; last known rate is used if the API is unavailable.

## Data Entities

- `Account` — AccountId, Code, Name, Type, ParentId, Currency, IsActive
- `JournalEntry` — EntryId, Date, Reference, Description, CreatedBy, CreatedAt, IsLocked, IsDeleted, Lines[]
- `JournalLine` — LineId, EntryId, AccountId, Debit, Credit, Currency, ExchangeRate, Memo

## Technical References

- SRS §3.1 (General Ledger Module)
- SDD §4.5 (Double-Entry Validation), §6.1–6.3 (Data Layer), §6.6 (Audit Trail)
