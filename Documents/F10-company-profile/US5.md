# US5: Grouped Accordion Navigation

**As any** user of the application,
**I want** the left-side navigation to be organised into labelled, collapsible groups,
**So that** I can quickly find the section I need without scrolling through an undifferentiated list of 15+ items.

## Acceptance Criteria

- [ ] The sidebar navigation is reorganised into the following named groups, each rendered as a collapsible MudNavGroup (accordion):

  | Group | Items (Standard mode) |
  |---|---|
  | **Overview** | Dashboard |
  | **Sales** | Customers, Invoices, AR Aging |
  | **Purchases** | Vendors, Bills, Payment Schedule, AP Aging, Expense Reports |
  | **Accounting** | General Ledger, Financial Reports, Budgets, Fixed Assets |
  | **Tax** | Tax & VAT, Margin Analysis |
  | *(divider + flat link)* | Settings |

- [ ] In **Auto-entrepreneur** mode the groups change as follows:
  - The **Tax** group is hidden entirely.
  - **Fixed Assets** and **Expense Reports** are removed from their groups.
  - **Financial Reports** within Accounting shows only P&L and Trial Balance (Balance Sheet tab hidden).

- [ ] Each group is expanded by default on first visit. Subsequent collapses or expansions are persisted per-user in a `UserNavPreferences` record (`CollapsedGroups: string[]`) so the sidebar remembers its state across page navigations and browser sessions.

- [ ] A group auto-expands when the user navigates to a route that belongs to it (e.g. navigating to `/invoicing` expands the Sales group even if it was manually collapsed).

- [ ] The active page link within any group is highlighted using the standard MudBlazor active-link style.

- [ ] On mobile / narrow viewports (< 960 px) the sidebar behaves identically to today (drawer, toggled by hamburger); the grouping is preserved inside the drawer.

- [ ] The Settings link remains outside any accordion group, separated by a divider, and always visible (not collapsible).

## Priority
Must Have

## Related Requirements
FR-CP-05
