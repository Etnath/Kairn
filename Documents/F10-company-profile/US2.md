# US2: Auto-Entrepreneur Invoice Compliance

**As an** auto-entrepreneur user,
**I want** my invoices to automatically conform to the legal requirements of the franchise en base de TVA regime,
**So that** I never hand a non-compliant invoice to a client and avoid the associated penalties.

## Acceptance Criteria

- [ ] When the tenant's business status is **Auto-entrepreneur**, the invoice creation and edit form hides the VAT rate selector on every line item.
- [ ] All line totals are computed without tax; the "Tax" column is removed from the line items table in both the UI and the PDF.
- [ ] The generated PDF invoice includes the mandatory legal mention on a dedicated line below the total: **"TVA non applicable — art. 293 B du CGI"**. This text is not editable by the user.
- [ ] The invoice PDF total section shows only the HT (Hors Taxes) total, with no TTC or VAT breakdown rows.
- [ ] The SIRET number from the Company Profile is printed in the invoice header.
- [ ] Existing invoices created before the status was set to Auto-entrepreneur are unaffected (their PDF still renders with the VAT amounts they were created with); only new invoices follow the new format.
- [ ] If the status is subsequently changed back to Standard, the VAT rate selector reappears and new invoices resume normal VAT handling.

## Priority
Must Have

## Related Requirements
FR-CP-02
