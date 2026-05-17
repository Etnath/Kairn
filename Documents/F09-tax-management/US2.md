# US2: Generate VAT Return Report

**As a** business owner or accountant,
**I want** to generate a VAT return report for any tax period,
**So that** I can see the net VAT payable (or refundable) and file my tax return accurately.

## Acceptance Criteria

- [ ] A "VAT Return" report is accessible under the Tax Management module.
- [ ] The user selects a tax period (a defined tax period from the system or a custom date range).
- [ ] The report displays:
  - Output Tax: total VAT charged on sales invoices (AR) in the period.
  - Input Tax: total VAT paid on vendor bills (AP) in the period.
  - Net VAT Payable: Output Tax − Input Tax.
- [ ] A positive Net VAT Payable means the business owes tax; a negative value means a refund is due (shown in green).
- [ ] The report breaks down output and input tax by VAT rate category (Standard, Reduced, Exempt).
- [ ] Clicking any amount drills down to the underlying invoices or bills contributing to that figure.
- [ ] The report can be exported as PDF and CSV.
- [ ] The report filters out transactions in locked tax periods from editable status but includes their amounts.

## Priority
Must Have

## Related Requirements
FR-TX-02
