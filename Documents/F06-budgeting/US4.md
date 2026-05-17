# US4: Copy Prior Year Budget

**As a** business owner,
**I want** to copy the previous year's budget as a starting point for the new year,
**So that** I don't have to re-enter the entire budget from scratch when the amounts are similar year-over-year.

## Acceptance Criteria

- [ ] When creating a new budget for a fiscal year, a "Copy from Prior Year" option is available if a budget exists for the previous fiscal year.
- [ ] Selecting this option pre-fills all monthly amounts from the prior year's budget; the user can edit any cell before saving.
- [ ] An optional "Apply inflation factor %" field scales all amounts by the entered percentage before pre-filling (e.g. +3% applies a 1.03 multiplier to each cell).
- [ ] The copy is applied in-memory until the user explicitly saves the new budget; cancelling discards the copy.
- [ ] A confirmation message is shown after the copy: "Budget pre-filled from [Year] budget. Review and save your changes."
- [ ] This action is only available during budget creation, not for editing an existing budget.

## Priority
Nice to Have

## Related Requirements
FR-BU-04
