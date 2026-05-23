# Feature: Company Profile & Business Status

## Overview

The Company Profile module lets an administrator configure the business's legal status and identity once, and the system adapts its UI, workflows, and compliance requirements accordingly. The first supported statuses are **Standard** (a VAT-registered company with full accounting obligations) and **Auto-entrepreneur** (the French micro-enterprise regime with simplified obligations and VAT exemption below the franchise threshold).

A companion change ships in the same feature: the left-side navigation is reorganised into labelled, collapsible accordion groups so the sidebar stays manageable as the product grows.

## Goals

- Eliminate irrelevant screens and fields for auto-entrepreneurs (VAT returns, tax periods, balance sheet, double-entry GL) without removing functionality for standard businesses.
- Ensure every auto-entrepreneur invoice is legally compliant: correct exemption mention, zero VAT lines, sequential numbering, and all mandatory fields.
- Alert the user before they cross the franchise en base de TVA threshold and trigger compulsory VAT registration.
- Provide a clean, grouped navigation that scales without overwhelming any type of user.

## Requirements

| ID | Requirement | Priority |
|---|---|---|
| FR-CP-01 | Company profile settings page: legal name, SIRET, address, business status (Standard / Auto-entrepreneur), activity type (services / commercial), VAT threshold override | Must Have |
| FR-CP-02 | Auto-entrepreneur invoice compliance: invoices show "TVA non applicable — art. 293 B du CGI", zero-rate VAT lines are hidden, tax rate selector is hidden | Must Have |
| FR-CP-03 | TVA threshold alert: configurable revenue ceiling per activity type; dashboard KPI and in-app notification when YTD revenue reaches 80 % and 100 % of threshold | Must Have |
| FR-CP-04 | Adaptive navigation: hide modules irrelevant to auto-entrepreneurs (Tax/VAT, Balance Sheet, Tax period locking, Fixed Assets, Expense Reports approval workflow) | Must Have |
| FR-CP-05 | Grouped accordion navigation: reorganise the left sidebar into named, collapsible groups for all business statuses; persist the open/closed state per user | Must Have |

## Key Domain Rules

- The business status is a **tenant-level setting** stored in a new `TenantProfile` entity; changing it takes effect immediately for all users of that tenant.
- Auto-entrepreneur VAT exemption applies only while cumulative annual revenue (invoiced, not necessarily collected) stays below the statutory threshold:
  - Services / liberal professions: **77 700 €** (2024; adjustable in settings)
  - Commercial / accommodation: **188 700 €** (2024; adjustable in settings)
- The exemption mention "TVA non applicable — art. 293 B du CGI" is **mandatory** on every invoice regardless of amount; its absence is a legal infraction.
- If the business status is Standard, no UI elements are suppressed; the full feature set is available.
- Navigation groups are defined in application code (not configurable by users); the user can collapse/expand each group and the choice is persisted.
- Fixed assets and double-entry journal entries remain accessible to auto-entrepreneurs if they choose to use them (they are not strictly required by law but are not harmful); only the VAT-specific modules are hidden.

## Navigation Groups (F10-US5)

| Group Label | Items |
|---|---|
| **Overview** | Dashboard |
| **Sales** | Customers, Invoices, Receivables (AR Aging) |
| **Purchases** | Vendors, Bills, Payment Schedule, AP Aging, Expense Reports |
| **Accounting** | General Ledger, Financial Reports, Budgets, Fixed Assets |
| **Tax** | Tax & VAT, Margin Analysis |
| *(divider)* | Settings |

Auto-entrepreneur mode hides the **Tax** group entirely and replaces **Accounting** with a single **Records** item (livre des recettes).

## Data Entities

- `TenantProfile` — TenantId (PK/FK), LegalName, Siret, Address, BusinessStatus (enum: Standard | AutoEntrepreneur), ActivityType (enum: Services | Commercial), VatThresholdOverride?, LogoPath?
- `UserNavPreferences` — UserId, CollapsedGroups (JSON string[])

## Technical References

- SRS §2.3 (User Classes), §3.8 (Tax Management)
- SDD §3.2 (Settings), §5.5 (PDF — QuestPDF), §6.1 (Data Layer)
- French tax law: CGI art. 293 B (franchise en base), BOFiP IS — TVA — CHAMP — 10 — 10 — 30
