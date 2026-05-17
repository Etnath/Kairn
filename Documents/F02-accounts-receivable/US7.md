# US7: Credit Notes

**As a** bookkeeper or business owner,
**I want** to issue a credit note against a previously sent invoice,
**So that** I can correct billing errors or process returns while maintaining an accurate audit trail.

## Acceptance Criteria

- [ ] A "Credit Note" button is available on any invoice with status Sent, Partially Paid, Paid, or Overdue.
- [ ] The credit note form is pre-filled with the original invoice's customer, lines, and amounts; all fields are editable.
- [ ] The credit note amount must be positive and cannot exceed the original invoice total; an error is shown if it does.
- [ ] Saving the credit note posts a reversing journal entry (debit Revenue, credit AR) and links it to the original invoice.
- [ ] The credit note reduces the outstanding balance of the original invoice; if the credit equals the outstanding balance, the invoice status becomes **Paid**.
- [ ] Credit notes are listed in the invoice list with a "Credit Note" label and a negative amount.
- [ ] Clicking a credit note shows which original invoice it is linked to.
- [ ] If a credit note results in a net overpayment (credit > outstanding balance), the difference is flagged as a customer credit balance to be refunded or applied to a future invoice.

## Priority
Must Have

## Related Requirements
FR-AR-07
