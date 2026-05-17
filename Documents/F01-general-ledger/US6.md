# US6: Recurring Journal Entries

**As a** bookkeeper or business owner,
**I want** to define journal entries that repeat on a schedule,
**So that** regular transactions like monthly rent or depreciation are posted automatically without manual re-entry each period.

## Acceptance Criteria

- [ ] On the General Ledger page, an Admin or Bookkeeper can create a "Recurring Entry" template with all the standard journal entry fields plus: frequency (Daily, Weekly, Monthly, Quarterly, Annually), start date, and optional end date.
- [ ] Recurring templates appear in a separate "Recurring Entries" tab, showing name, frequency, last posted date, next due date, and active status.
- [ ] The system automatically posts a journal entry from the template on each due date via a background scheduled job.
- [ ] A manually triggered "Post Now" button on a template allows the user to post the next occurrence ahead of schedule.
- [ ] If an automated posting fails (e.g. the account is inactive), the error is logged and an alert is added to the alerts panel.
- [ ] A recurring template can be deactivated, which prevents future postings without deleting past ones.
- [ ] Each automatically posted entry references the template ID in its description and is tagged `IsRecurring = true`.
- [ ] All recurring postings are included in the audit trail with `CreatedBy = "System"`.

## Notes

- The background job runs daily at a configurable time (default 00:30 server time) using a hosted `IHostedService`.
- End date is inclusive; the last posting occurs on or before the end date.

## Priority
Should Have

## Related Requirements
FR-GL-06, FR-GL-07
