# Kairn — Financial Management ERP
## Software Requirements Specification
**Version 1.0 · May 2026 · Draft — Internal Use Only**

---

| Field | Value |
|---|---|
| Product Name | Kairn — Financial Management ERP |
| Version | 1.0 — Initial Release |
| Date | May 2026 |
| Status | Draft — Pending Review |
| Audience | Development Team, Stakeholders |

---

## Table of Contents

1. [Introduction](#1-introduction)
2. [Overall Description](#2-overall-description)
3. [Functional Requirements](#3-functional-requirements)
4. [Non-Functional Requirements](#4-non-functional-requirements)
5. [System Interfaces](#5-system-interfaces)
6. [Appendices](#6-appendices)

---

## 1. Introduction

### 1.1 Purpose

This Software Requirements Specification (SRS) defines the functional and non-functional requirements for **Kairn**, a small-business financial ERP. The system supports day-to-day financial operations including double-entry bookkeeping, profit & loss analysis, balance sheet tracking, and financial reporting.

This document serves as the definitive reference for the development team and guides design, implementation, and validation.

### 1.2 Scope

Kairn is a web-based ERP application tailored for small business owners with no advanced accounting background. The system provides:

- Full double-entry accounting ledger
- Real-time profit & loss (P&L) reporting
- Balance sheet with assets, liabilities, and equity
- Cash flow tracking and forecasting
- Accounts receivable and payable management
- Margin analysis per product or service category
- Tax preparation support (VAT/TVA, income reporting)
- Multi-currency support (CHF, EUR, USD, GBP)
- Dashboard with key financial KPIs

### 1.3 Definitions and Acronyms

| Term | Definition |
|---|---|
| ERP | Enterprise Resource Planning — integrated business management software |
| SRS | Software Requirements Specification |
| P&L | Profit & Loss statement |
| AR / AP | Accounts Receivable / Accounts Payable |
| COA | Chart of Accounts |
| GAAP | Generally Accepted Accounting Principles |
| KPI | Key Performance Indicator |
| CRUD | Create, Read, Update, Delete |
| VAT / TVA | Value Added Tax (European standard) |
| IFRS | International Financial Reporting Standards |

### 1.4 References

- International Financial Reporting Standards (IFRS)
- Code de commerce français — Obligations comptables (Art. L123-12 et seq.)
- Plan Comptable Général (PCG) — Règlement ANC n°2014-03
- ISO/IEC 25010:2011 — Software Quality Model
- IEEE 830-1998 — Recommended Practice for SRS

### 1.5 Overview

Section 2 describes the product context and constraints. Section 3 contains all functional requirements by module. Section 4 defines non-functional requirements. Section 5 covers system interfaces. Section 6 contains appendices and data models.

---

## 2. Overall Description

### 2.1 Product Perspective

Kairn is a standalone web application, not dependent on any existing ERP system. It stores all financial data on a private server with optional cloud backup. The system is designed for a single-entity small business operating primarily in France, with multi-currency support.

### 2.2 Product Functions — High-Level Summary

| Module | Key Capabilities |
|---|---|
| General Ledger | Chart of accounts, double-entry journal entries, account reconciliation |
| Financial Reporting | P&L statement, balance sheet, cash flow statement, custom date ranges |
| Accounts Receivable | Customer invoicing, payment tracking, aging reports, reminders |
| Accounts Payable | Vendor bills, payment scheduling, expense categorization |
| Budgeting | Annual budgets by category, variance analysis, forecasting |
| Margin Analysis | Revenue and cost per product/service line, gross/net margin |
| Tax Management | VAT computation, tax period reports, export for accountant |
| Dashboard | Live KPI widgets, charts, alerts for overdue items |
| Data Management | Import/export CSV, backup, audit trail, multi-user access |

### 2.3 User Classes and Characteristics

**Business Owner (Primary User)**
The main user of the system. May have limited accounting knowledge. Requires an intuitive UI with guided workflows, contextual help, and visual summaries. Responsible for entering transactions, reviewing reports, and managing settings.

**Accountant / Bookkeeper (Secondary User)**
A professional user who performs period-end reconciliations, tax preparation, and audits. Requires full access to journal entries, trial balances, and export features.

**Administrator**
Manages user accounts, system configuration, backup schedules, and access control. May be the same person as the business owner.

### 2.4 Operating Environment

- **Platform:** Any modern web browser (Chrome, Firefox, Safari, Edge)
- **Database:** PostgreSQL (server deployment); SQLite for development/testing
- **Screen resolution:** minimum 1280 × 768; responsive layout for tablets
- **Internet:** required for normal use; offline WASM mode planned for Phase 2

### 2.5 Design and Implementation Constraints

- All monetary values must be stored as 64-bit decimal to avoid floating-point rounding errors
- The system must enforce double-entry accounting — every transaction must balance (debits = credits)
- The fiscal year must be configurable (e.g. January–December or July–June)
- The system must support French accounting standards (PCG / Code de commerce) and optionally IFRS
- All data must be exportable in CSV and PDF formats
- The application must function correctly on the latest two major versions of each supported browser

### 2.6 Assumptions and Dependencies

- The business operates as a single legal entity (auto-entrepreneur, EURL, SARL, SAS, etc.)
- A real-time currency API (e.g. Frankfurter, ECB) is used for exchange rates
- Tax rates are configurable and not hard-coded
- The server environment supports Docker and PostgreSQL 15+

---

## 3. Functional Requirements

### Priority Legend

| Priority | Meaning |
|---|---|
| **Must Have** | Required for MVP. Cannot launch without. |
| **Should Have** | High value; include unless significant time or cost constraints apply. |
| **Nice to Have** | Desirable, can be deferred to a future release. |

---

### 3.1 General Ledger Module

The General Ledger is the core of the system. All financial transactions are recorded using the double-entry method.

| ID | Requirement | Description | Priority |
|---|---|---|---|
| FR-GL-01 | Chart of Accounts | Configurable COA organized by type: Assets, Liabilities, Equity, Revenue, Expenses. Each account has a code, name, type, and currency. | Must Have |
| FR-GL-02 | Journal Entry Creation | Create manual journal entries with one or more debit/credit lines. System rejects entries where total debits ≠ total credits. | Must Have |
| FR-GL-03 | Transaction Reference | Every entry has a unique reference number, date, description, and optional attachment (PDF/image receipt). | Must Have |
| FR-GL-04 | Account Reconciliation | Reconcile bank accounts by matching transactions against imported bank statements. | Must Have |
| FR-GL-05 | Trial Balance | Generate a Trial Balance report showing all account balances at any selected date. | Must Have |
| FR-GL-06 | Recurring Entries | Define recurring journal entries (e.g. monthly rent) with configurable frequency and end date. | Should Have |
| FR-GL-07 | Audit Trail | All created, modified, or deleted entries are logged with timestamp and user identity. Deletion must be soft (reversible). | Must Have |
| FR-GL-08 | Multi-Currency Entries | Journal entries support amounts in multiple currencies, with automatic conversion to the base currency using the exchange rate at the transaction date. | Should Have |

---

### 3.2 Profit & Loss Module

The P&L module provides real-time income statement analysis across configurable time periods.

| ID | Requirement | Description | Priority |
|---|---|---|---|
| FR-PL-01 | P&L Statement | Generate a Profit & Loss statement for any user-defined period, showing Revenue, COGS, Gross Profit, Operating Expenses, EBITDA, and Net Income. | Must Have |
| FR-PL-02 | Period Comparison | Compare P&L across two periods side-by-side (e.g. this month vs. last month, this year vs. last year). | Must Have |
| FR-PL-03 | Category Drill-Down | Clicking any P&L line item drills down to the underlying transactions. | Must Have |
| FR-PL-04 | Budget vs. Actual | Display P&L figures alongside budget figures with variance shown in absolute and percentage terms. | Should Have |
| FR-PL-05 | Export | P&L reports exportable as PDF and CSV. | Must Have |

---

### 3.3 Balance Sheet Module

The balance sheet provides a snapshot of assets, liabilities, and equity at any point in time.

| ID | Requirement | Description | Priority |
|---|---|---|---|
| FR-BS-01 | Balance Sheet Report | Generate a Balance Sheet (Assets = Liabilities + Equity) for any selected date, organized by current and non-current categories. | Must Have |
| FR-BS-02 | Asset Register | Maintain a register of fixed assets including purchase date, value, depreciation method (straight-line or declining balance), and net book value. | Must Have |
| FR-BS-03 | Depreciation Automation | Automatically calculate and post monthly depreciation journal entries for fixed assets. | Must Have |
| FR-BS-04 | Equity Tracking | Track owner equity movements including capital contributions, withdrawals, and retained earnings. | Must Have |
| FR-BS-05 | Comparative Balance Sheet | Compare balance sheets between two dates. | Should Have |

---

### 3.4 Accounts Receivable Module

| ID | Requirement | Description | Priority |
|---|---|---|---|
| FR-AR-01 | Customer Management | Maintain a customer database with contact details, payment terms, and credit limit. | Must Have |
| FR-AR-02 | Invoice Creation | Create itemized invoices with product/service lines, quantities, unit prices, discounts, and tax amounts. | Must Have |
| FR-AR-03 | Invoice Status Tracking | Invoices have statuses: Draft, Sent, Partially Paid, Paid, Overdue, Void. | Must Have |
| FR-AR-04 | Payment Recording | Record full or partial payments against an invoice, automatically updating the outstanding balance. | Must Have |
| FR-AR-05 | Aging Report | Generate an AR aging report (current, 30/60/90/90+ days overdue) by customer. | Must Have |
| FR-AR-06 | Payment Reminders | Generate overdue payment reminder notices that can be printed or emailed. | Should Have |
| FR-AR-07 | Credit Notes | Issue credit notes against previously issued invoices. | Must Have |

---

### 3.5 Accounts Payable Module

| ID | Requirement | Description | Priority |
|---|---|---|---|
| FR-AP-01 | Vendor Management | Maintain a vendor database with payment terms, contact details, and IBAN/bank details. | Must Have |
| FR-AP-02 | Bill Entry | Enter vendor bills with line items, due dates, and expense categories. | Must Have |
| FR-AP-03 | Payment Scheduling | Show upcoming payables and allow scheduling of payments. | Must Have |
| FR-AP-04 | Bill Approval Workflow | Bills above a configurable threshold require approval before payment. | Nice to Have |
| FR-AP-05 | AP Aging Report | Generate an AP aging report showing amounts owed by due date. | Must Have |
| FR-AP-06 | Expense Reports | Submit expense reports with receipts attached. | Should Have |

---

### 3.6 Margin Analysis Module

| ID | Requirement | Description | Priority |
|---|---|---|---|
| FR-MA-01 | Product/Service Lines | Define product or service categories for revenue and cost tracking. | Must Have |
| FR-MA-02 | Gross Margin Report | Compute gross margin (Revenue − COGS) per product/service line. | Must Have |
| FR-MA-03 | Net Margin Report | Compute net margin after allocating operating expenses. | Should Have |
| FR-MA-04 | Margin Trend | Display margin trends over time as a line chart. | Should Have |
| FR-MA-05 | Margin Alerts | Alert the user when gross margin on any category falls below a configurable threshold. | Nice to Have |

---

### 3.7 Dashboard Module

| ID | Requirement | Description | Priority |
|---|---|---|---|
| FR-DB-01 | KPI Cards | Display KPI cards for: Monthly Revenue, Monthly Expenses, Net Profit, Outstanding AR, Outstanding AP, and Cash Balance. | Must Have |
| FR-DB-02 | Charts | Include a revenue vs. expenses bar chart (monthly), a P&L trend line chart, and a cash position chart. | Must Have |
| FR-DB-03 | Alerts Panel | Display alerts for overdue invoices, upcoming payables, and low cash balance. | Must Have |
| FR-DB-04 | Customizable Layout | Users can choose which KPI widgets appear on their dashboard. | Nice to Have |

---

### 3.8 Tax Management Module

| ID | Requirement | Description | Priority |
|---|---|---|---|
| FR-TX-01 | VAT Configuration | Configure TVA rates by category (e.g. 20% normal, 10% intermédiaire, 5.5% réduit, 2.1% super-réduit in France). | Must Have |
| FR-TX-02 | VAT Return Report | Generate a VAT return report for any period showing input tax, output tax, and net payable. | Must Have |
| FR-TX-03 | Tax Period Locking | Lock a tax period to prevent modification of transactions after filing. | Must Have |
| FR-TX-04 | Accountant Export | Export a complete transaction file (journal entries, P&L, balance sheet) in Excel and PDF for the external accountant. | Must Have |

---

### 3.9 Budgeting Module

| ID | Requirement | Description | Priority |
|---|---|---|---|
| FR-BU-01 | Budget Creation | Create annual budgets by account or category, broken down by month. | Must Have |
| FR-BU-02 | Variance Analysis | Display budget vs. actual variance in absolute and percentage terms for any period. | Must Have |
| FR-BU-03 | Budget Forecasting | Project year-end figures based on actuals to date plus remaining budget. | Should Have |
| FR-BU-04 | Budget Copy | Copy a previous year's budget as the starting point for the next year. | Nice to Have |

---

## 4. Non-Functional Requirements

### 4.1 Performance

| ID | Requirement | Description | Priority |
|---|---|---|---|
| NFR-P-01 | Report Generation | All standard reports (P&L, Balance Sheet) generate in under 3 seconds for up to 100,000 transactions. | Must Have |
| NFR-P-02 | UI Responsiveness | All UI interactions respond within 500ms under normal load. | Must Have |
| NFR-P-03 | Concurrent Users | The system must support at least 10 simultaneous users without degradation. | Should Have |

### 4.2 Security

| ID | Requirement | Description | Priority |
|---|---|---|---|
| NFR-S-01 | Authentication | Username and password login with PBKDF2-SHA256 hashed password storage. | Must Have |
| NFR-S-02 | Role-Based Access Control | Roles: Admin, Bookkeeper, Viewer. Permissions enforced per module. | Must Have |
| NFR-S-03 | Transport Encryption | All network traffic uses TLS 1.2+. HTTPS enforced; HSTS header present. | Must Have |
| NFR-S-04 | Audit Trail | All data modifications logged with user, timestamp, and changed values. | Must Have |
| NFR-S-05 | Session Timeout | User sessions expire after a configurable period of inactivity (default: 30 minutes). | Must Have |
| NFR-S-06 | Secret Management | Database credentials and API keys stored via environment variables or a secrets manager; never in source control. | Must Have |

### 4.3 Usability

| ID | Requirement | Description | Priority |
|---|---|---|---|
| NFR-U-01 | Learnability | A new user with basic accounting knowledge completes core workflows without training in under 2 hours. | Must Have |
| NFR-U-02 | Accessibility | UI conforms to WCAG 2.1 Level AA. | Should Have |
| NFR-U-03 | Localization | UI supports English and French. Number formats and currency symbols adapt to locale. | Must Have |
| NFR-U-04 | Help System | Context-sensitive help and tooltips available throughout the application. | Should Have |
| NFR-U-05 | Responsive Design | Application is usable on tablet-sized screens (768px+) in addition to desktop. | Should Have |

### 4.4 Reliability and Data Integrity

| ID | Requirement | Description | Priority |
|---|---|---|---|
| NFR-R-01 | Data Backup | Automatic daily backups with manual on-demand backup option. Point-in-time recovery via WAL. | Must Have |
| NFR-R-02 | Transaction Atomicity | All multi-step financial operations are atomic — fully committed or fully rolled back. | Must Have |
| NFR-R-03 | Double-Entry Enforcement | System rejects any journal entry where debits do not equal credits to the last decimal place. | Must Have |
| NFR-R-04 | Availability | System targets 99.5% uptime for the server deployment. | Must Have |

### 4.5 Maintainability

| ID | Requirement | Description | Priority |
|---|---|---|---|
| NFR-M-01 | Modular Architecture | Layered architecture (Presentation, Application, Domain, Infrastructure) with clear separation of concerns. | Must Have |
| NFR-M-02 | Automated Tests | Business logic achieves minimum 80% unit test coverage. | Must Have |
| NFR-M-03 | Logging | Structured application logging for errors, warnings, and audit events (Serilog). | Must Have |
| NFR-M-04 | Containerization | Application deployable as a Docker container. Database migrations run automatically on startup. | Must Have |

---

## 5. System Interfaces

### 5.1 User Interface

The UI follows a consistent design language across all modules using MudBlazor Material Design components. Navigation is provided via a left-side drawer menu. All data entry forms include inline validation with error messages. All tables support sorting, filtering, and pagination.

### 5.2 Currency Exchange Rate API

Kairn integrates with a public exchange rate API (e.g. `api.frankfurter.app`) to fetch daily exchange rates for CHF, EUR, USD, and GBP. Rates are cached locally and refreshed daily. If the API is unavailable, the last known rate is used and the user is notified.

### 5.3 Bank Statement Import

The system supports import of bank statements in OFX, QFX, and CSV formats for account reconciliation.

### 5.4 Email (Optional)

Optional SMTP integration allows invoices and payment reminders to be sent directly from the application.

### 5.5 PDF Generation

All reports and invoices are exportable as PDF using QuestPDF (server-side, no external dependencies).

### 5.6 Database

The system uses Entity Framework Core with a versioned migration strategy. PostgreSQL is the default for server deployment; SQLite is supported for development and testing. Migrations are applied automatically on startup.

---

## 6. Appendices

### 6.1 Core Data Entities

| Entity | Key Attributes |
|---|---|
| Account | AccountId, Code, Name, Type (Asset / Liability / Equity / Revenue / Expense), ParentId, Currency, IsActive |
| JournalEntry | EntryId, Date, Reference, Description, CreatedBy, CreatedAt, IsLocked, IsDeleted, Lines[] |
| JournalLine | LineId, EntryId, AccountId, Debit, Credit, Currency, ExchangeRate, Memo |
| Customer | CustomerId, Name, Email, Address, TaxNumber, PaymentTermsDays, CreditLimit, Currency |
| Invoice | InvoiceId, CustomerId, Date, DueDate, Status, Lines[], TaxAmount, TotalAmount, Currency |
| Vendor | VendorId, Name, ContactEmail, IBAN, PaymentTermsDays, DefaultExpenseAccountId |
| Bill | BillId, VendorId, Date, DueDate, Status, Lines[], TaxAmount, TotalAmount |
| FixedAsset | AssetId, Name, Category, PurchaseDate, PurchaseValue, DepreciationMethod, UsefulLifeYears |
| BudgetLine | BudgetId, AccountId, FiscalYear, Month, Amount |
| TaxRate | TaxRateId, Name, Rate, Category, IsDefault, ValidFrom, ValidTo |
| AuditLog | LogId, EntityType, RecordId, Action, ChangedBy, ChangedAt, OldValues, NewValues |

### 6.2 Revision History

| Version | Date | Author | Summary |
|---|---|---|---|
| 1.0 | May 2026 | Project Lead | Initial draft |

---

*Kairn — navigate your finances with confidence.*
