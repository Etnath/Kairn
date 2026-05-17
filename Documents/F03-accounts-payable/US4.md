# US4: Bill Approval Workflow

**As an** administrator,
**I want** to require explicit approval for bills above a configurable monetary threshold,
**So that** large purchases are reviewed before payment is authorised.

## Acceptance Criteria

- [ ] In Settings → Accounts Payable, an Admin can configure: approval threshold amount (CHF), and which roles can approve (default: Admin only).
- [ ] When a bill total exceeds the threshold, it is saved as **Pending Approval** instead of going directly to Approved.
- [ ] An approval request notification is displayed in the alerts panel for users with the approver role.
- [ ] An approver can view the pending bill and click "Approve" or "Reject".
- [ ] Approving the bill transitions it to **Approved** and posts the journal entries.
- [ ] Rejecting the bill transitions it to **Rejected** with a mandatory reason field; the submitter is notified via the alerts panel.
- [ ] A rejected bill can be edited and resubmitted, which returns it to **Pending Approval**.
- [ ] All approval and rejection events are recorded in the audit log with the user and timestamp.
- [ ] Bills below the threshold skip the approval step and go directly to **Approved** (if auto-post is enabled) or remain in **Draft**.

## Priority
Nice to Have

## Related Requirements
FR-AP-04
