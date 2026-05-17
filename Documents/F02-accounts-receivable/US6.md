# US6: Overdue Payment Reminders

**As a** business owner,
**I want** to generate and send payment reminder notices to customers with overdue invoices,
**So that** I can chase unpaid invoices professionally without manually drafting reminder letters.

## Acceptance Criteria

- [ ] A "Send Reminder" button is available on any invoice with status Overdue.
- [ ] Clicking the button opens a preview of the reminder notice showing: customer name and address, invoice reference, original due date, outstanding amount, and a standard reminder message.
- [ ] The reminder message text is editable before sending.
- [ ] The user can print the reminder (browser print dialog) or send it via email (if SMTP is configured).
- [ ] Sending via email attaches the original invoice PDF and sends to the customer's email on record.
- [ ] A "Reminder Sent" event is logged against the invoice with the date and sending user; it is visible on the invoice detail page.
- [ ] A bulk "Send All Reminders" action on the AR Aging report generates reminders for all overdue customers in one step, with a confirmation dialog listing the recipients.

## Priority
Should Have

## Related Requirements
FR-AR-06, §5.4 (SMTP)
