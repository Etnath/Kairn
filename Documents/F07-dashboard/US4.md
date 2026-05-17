# US4: Customisable Dashboard Layout

**As a** business owner,
**I want** to choose which KPI widgets appear on my dashboard,
**So that** the dashboard shows only the metrics most relevant to my daily workflow.

## Acceptance Criteria

- [ ] A "Customise" button on the dashboard opens a settings panel listing all available KPI widgets with toggle switches.
- [ ] The user can show or hide any of the six standard KPI cards individually.
- [ ] Widget visibility preferences are saved per user and persist across sessions (stored in the user profile table).
- [ ] At least one widget must remain visible; disabling the last active widget shows a validation message.
- [ ] Hidden widgets can be re-enabled at any time from the same settings panel.
- [ ] The Customise panel does not affect charts or the alerts panel (those are always visible).
- [ ] The layout reflows gracefully when fewer than six cards are displayed (cards fill available width).

## Priority
Nice to Have

## Related Requirements
FR-DB-04
