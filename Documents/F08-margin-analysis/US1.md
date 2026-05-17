# US1: Define Product and Service Lines

**As a** business owner,
**I want** to define product or service categories and link them to revenue and cost accounts,
**So that** the system can compute profitability per line of business.

## Acceptance Criteria

- [ ] A "Product Lines" settings page is accessible to Admin under Settings or Margin Analysis.
- [ ] An Admin can create a product/service line with fields: name (required), description, linked revenue account(s) (COA multi-select), linked COGS account(s) (COA multi-select), and margin alert threshold % (optional).
- [ ] An existing product line can be edited or deactivated; deactivation hides it from margin reports going forward but preserves history.
- [ ] Product line names must be unique; a validation error is shown on duplicate submission.
- [ ] All product line changes are recorded in the audit log.
- [ ] At least one product line must exist before the Margin Analysis report can be generated; a prompt directs the user to create one if none exist.

## Priority
Must Have

## Related Requirements
FR-MA-01, FR-MA-05
