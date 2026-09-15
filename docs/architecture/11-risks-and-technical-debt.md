# 11. Risks and Technical Debt

## Risks

<!-- Known technical risks, ordered by priority -->

| Risk | Probability | Impact | Mitigation |
| ---- | ----------- | ------ | ---------- |
| **There is no authentication.** The current user is a cookie-driven stub ([ADR-0004](adr/0004-stubbed-identity-until-authentication-lands.md)); anyone can become a Manager by setting `dev-role`. | Certain — this is the current state | Critical if exposed. Every Manager-only rule (FR-018) is bypassable. | Do not deploy anywhere untrusted users can reach. Spec 001 §10 Q1 must be answered and `ICurrentUser` given a real implementation before any deployment. |
| SQLite allows one writer at a time. Concurrent managers editing the same data can hit `SQLITE_BUSY`. | Low at the scale in spec 001 NFR-005 (~100 people) | Failed write, shown as an error | Accepted for now ([ADR-0001](adr/0001-ef-core-with-sqlite-for-persistence.md)). The escape route is a provider swap to PostgreSQL. |
| Burned hours are re-aggregated on every project-list view. Growth past ~50 000 entries a year erodes the NFR-001 headroom. | Low near-term, rising over years | Slower project list | Measured: `SuccessCriteria.SUC05_ProjectListScales` asserts one query and < 500 ms at 200 projects / 50 000 entries. If it ever fails, the fix is a store-maintained summary, not a hand-maintained column ([ADR-0002](adr/0002-burned-hours-derived-never-stored.md)). |
| "Last write wins" on concurrent project edits — there is no optimistic-concurrency token. | Low | A manager's budget change silently overwritten | Accepted by spec 001 EC-8. `updatedAt` is recorded (NFR-007) so a surprising budget can at least be dated. Add a row version if it bites. |

## Technical Debt

<!-- Known technical debt items -->

| Item | Description | Effort | Priority |
| ---- | ----------- | ------ | -------- |
| Real authentication | Replace `DevRoleSwitchCurrentUser` and remove the role switcher from `_Layout.cshtml`. Also needs a link between the signed-in user and a `Person` row, which does not exist yet. | Medium | High — blocks any deployment |
| `TimeEntry` / `TimesheetWeek` are half-built | Both entities exist because spec 001 must read them to compute burned hours, shaped to specs 005 and 010 but with none of those specs' rules enforced and no UI. Marked as such in their own files. | — | Resolved by building specs 005 and 010 |
| Validation messages use the current culture | Number formatting in a few messages (`{MaxMinutes / 60:N0}`) follows the server locale while the values themselves are parsed invariantly. Cosmetic today; a trap once the app is localised. | Small | Low |
| Deployment story | `07-deployment-view.md` is empty. A file-backed SQLite database is a development convenience, not a deployment plan. | Medium | Medium |
| arc42 sections 01–08, 10 are empty templates | Spec 001 NFR-006/NFR-005 and similar project-wide qualities should move to `10-quality-requirements.md` and be referenced from the specs rather than restated. | Medium | Medium |
