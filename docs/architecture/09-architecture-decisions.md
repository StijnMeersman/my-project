# 9. Architecture Decisions

<!-- Important, expensive, large-scale, or risky architecture decisions.
     Individual ADRs are stored in docs/architecture/adr/ -->

## Decision Log

| ID  | Decision | Status | Date |
| --- | -------- | ------ | ---- |
| [ADR-0001](adr/0001-ef-core-with-sqlite-for-persistence.md) | EF Core with SQLite for persistence | Accepted | 2026-09-15 |
| [ADR-0002](adr/0002-burned-hours-derived-never-stored.md) | Burned hours are derived on every read, never stored | Accepted | 2026-09-15 |
| [ADR-0003](adr/0003-durations-as-whole-minutes.md) | Durations are whole minutes, not decimal hours | Accepted | 2026-09-15 |
| [ADR-0004](adr/0004-stubbed-identity-until-authentication-lands.md) | Identity is a stub behind `ICurrentUser` until authentication lands | Accepted — temporary | 2026-09-15 |

See [adr/](adr/) for full Architecture Decision Records.
