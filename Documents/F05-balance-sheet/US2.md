# US2: Fixed Asset Register

**As a** business owner or bookkeeper,
**I want** to maintain a register of fixed assets with their purchase details and depreciation parameters,
**So that** the system can automatically track the book value of each asset over time.

## Acceptance Criteria

- [ ] A "Fixed Assets" sub-section is available under the Balance Sheet module or Settings.
- [ ] An Admin or Bookkeeper can add a fixed asset with fields: name (required), category, purchase date (required), purchase value (required), residual value (default 0), depreciation method (Straight-Line or Declining Balance), useful life in years (required), linked asset account (COA), linked accumulated depreciation account (COA).
- [ ] The asset register table shows all active assets with columns: name, category, purchase date, purchase value, accumulated depreciation, net book value, and next depreciation date.
- [ ] Net book value is computed as: `Purchase Value − Accumulated Depreciation`.
- [ ] An asset can be edited (depreciation parameters only) until the first depreciation posting is made.
- [ ] Disposing of an asset (marking it inactive) posts a final disposal journal entry and removes it from active depreciation.
- [ ] Disposed assets are viewable in an "Inactive Assets" tab.
- [ ] All asset record changes are captured in the audit log.

## Priority
Must Have

## Related Requirements
FR-BS-02, FR-BS-03
