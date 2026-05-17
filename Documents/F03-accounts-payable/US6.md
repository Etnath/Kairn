# US6: Expense Reports

**As an** employee or business owner,
**I want** to submit expense reports with multiple line items and receipt attachments,
**So that** out-of-pocket business expenses are reimbursed accurately and posted to the correct accounts.

## Acceptance Criteria

- [ ] An "Expense Reports" sub-section is available within the Accounts Payable module.
- [ ] Any authenticated user can submit a new expense report with fields: title (required), submission date (defaults to today), and a line-item grid.
- [ ] Each line has: description (required), date of expense, amount, currency, expense category (COA account), and receipt attachment (PDF or image, up to 10 MB per line).
- [ ] The expense report total is computed from all line amounts converted to the base currency.
- [ ] On submission, the report status is **Pending Approval**; an approver is notified via the alerts panel.
- [ ] An Admin or Bookkeeper approver can approve or reject the report; rejection requires a reason.
- [ ] Approving the report posts journal entries: debit each Expense account (per line), credit Accounts Payable (or the submitter's employee payable account).
- [ ] A separate payment step marks the report as **Paid** and posts the final cash/bank journal entry.
- [ ] All expense report status changes are captured in the audit log.

## Priority
Should Have

## Related Requirements
FR-AP-06
