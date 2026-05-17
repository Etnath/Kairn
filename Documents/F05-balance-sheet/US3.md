# US3: Automated Monthly Depreciation

**As a** bookkeeper,
**I want** the system to automatically post depreciation journal entries for all active fixed assets at the end of each month,
**So that** I don't have to calculate and enter depreciation manually each period.

## Acceptance Criteria

- [ ] A background scheduled job runs on the last calendar day of each month (or configurable day) and calculates depreciation for all active assets.
- [ ] Straight-line monthly charge: `(Purchase Value − Residual Value) / (Useful Life Years × 12)`.
- [ ] Declining balance monthly charge: `Current Net Book Value × (Annual Rate / 12)`.
- [ ] The job posts one journal entry per asset per period: debit Depreciation Expense account, credit Accumulated Depreciation account.
- [ ] The journal entry description is: `"Depreciation — [Asset Name] — [Month Year]"` with reference `DEP-YYYYMM-NNN`.
- [ ] If an asset reaches zero net book value (fully depreciated), no further entries are posted and the asset is flagged `IsFullyDepreciated = true`.
- [ ] If the depreciation job fails for any asset, the failure is logged and an alert is added to the dashboard alerts panel; no partial posting is committed for that asset.
- [ ] A "Run Depreciation" manual trigger is available to Admin for out-of-cycle execution; it only processes months not yet posted.
- [ ] Depreciation postings are `IsRecurring = true` and attributed to `CreatedBy = "System"` in the audit log.

## Priority
Must Have

## Related Requirements
FR-BS-03, FR-GL-06, FR-GL-07
