# Feature: Accounts Receivable

## Overview

The Accounts Receivable module manages the full customer-to-cash lifecycle: customer records, invoice creation, payment tracking, aging analysis, overdue reminders, and credit notes. It integrates with the General Ledger so that every invoice and payment automatically posts the appropriate journal entries.

## Goals

- Enable the business to issue professional invoices and track what is owed by customers.
- Provide aging analysis so overdue balances are immediately visible.
- Reduce time spent chasing payments through automated reminder notices.
- Maintain accurate ledger balances through automatic GL posting for all AR events.

## Requirements

| ID | Requirement | Priority |
|---|---|---|
| FR-AR-01 | Customer database: contact details, payment terms, credit limit, currency | Must Have |
| FR-AR-02 | Itemised invoice creation: product/service lines, quantities, unit prices, discounts, tax | Must Have |
| FR-AR-03 | Invoice statuses: Draft, Sent, Partially Paid, Paid, Overdue, Void | Must Have |
| FR-AR-04 | Full or partial payment recording with automatic outstanding balance update | Must Have |
| FR-AR-05 | AR aging report: current, 30/60/90/90+ days overdue, by customer | Must Have |
| FR-AR-06 | Overdue payment reminder notices (printable or emailable via SMTP) | Should Have |
| FR-AR-07 | Credit notes issued against previously issued invoices | Must Have |

## Key Domain Rules

- An invoice transitions to **Overdue** automatically when the due date passes and the balance remains unpaid (evaluated nightly or on page load).
- Voiding an invoice posts a reversing journal entry to nullify the original posting.
- A credit note reduces the outstanding balance; if it exceeds the balance a refund entry is required.
- Payment terms default from the customer record but can be overridden per invoice.
- Tax (VAT) is computed per line using the applicable tax rate for the product/service category.

## Data Entities

- `Customer` — CustomerId, Name, Email, Address, TaxNumber, PaymentTermsDays, CreditLimit, Currency
- `Invoice` — InvoiceId, CustomerId, Date, DueDate, Status, Lines[], TaxAmount, TotalAmount, Currency
- `InvoiceLine` — InvoiceLineId, InvoiceId, Description, Quantity, UnitPrice, Discount, TaxRateId, LineTotal
- `Payment` (AR) — PaymentId, InvoiceId, Date, Amount, Currency, Reference

## Technical References

- SRS §3.4 (Accounts Receivable Module)
- SDD §3.2 (`Invoicing.razor`, `InvoiceForm.razor`, `StatusBadge.razor`), §5.4 (Email/SMTP)
