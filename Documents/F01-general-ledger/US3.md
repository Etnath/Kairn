# US3: Soft-Delete and Audit Trail for Journal Entries

**As an** accountant or administrator,
**I want** all journal entry creates, edits, and deletions to be fully logged and reversible,
**So that** the ledger provides a trustworthy, tamper-evident record for audit and compliance purposes.

## Acceptance Criteria

- [ ] Deleting a journal entry marks it `IsDeleted = true` in the database; the record is never physically removed.
- [ ] Deleted entries are hidden from all standard ledger views and reports by default.
- [ ] An Admin can view deleted entries via a "Show deleted" toggle on the General Ledger page.
- [ ] An Admin can restore a deleted entry (set `IsDeleted = false`); a restore event is written to the audit log.
- [ ] The audit log records for every entry mutation: entity type, record ID, action (Created / Updated / Deleted / Restored), changed-by user, timestamp, old values, and new values.
- [ ] The audit log is accessible to Admin via Settings → Audit Log with filtering by entity type, date range, and user.
- [ ] Audit log entries are immutable — no user role can edit or delete them.
- [ ] Locked entries (within a locked tax period) cannot be deleted or edited; attempting to do so returns an error: "This entry is in a locked period."

## Notes

- The `AuditLogInterceptor` (implemented in US0) covers this automatically for all EF Core entities.
- The "Show deleted" toggle should also indicate the count of deleted entries in the current filter view.

## Priority
Must Have

## Related Requirements
FR-GL-07, FR-TX-03, NFR-S-04
