# ADR-0001: EF Core with SQLite for persistence

| | |
| --- | --- |
| **Status** | Accepted |
| **Date** | 2026-09-15 |
| **Deciders** | Stijn |
| **Context** | [Spec 001 — Project Setup](../../specs/001-project-setup.md) §9.1, §9.3 |

## Context

Spec 001 is the first slice that stores anything. It notes that "no ADR exists yet" for persistence
and asks the first implementer to record the choice. Everything downstream depends on it: spec 005
writes a time entry per person/project/day, and spec 001's project list has to sum those entries on
every read (FR-013, NFR-003).

What the decision has to serve:

- **NFR-001** — the project list renders in under 500 ms with 200 projects and 50 000 time entries,
  computing burned hours with *one* aggregate query, not one per project. That rules out anything
  that cannot push a `SUM` with a `GROUP BY` down to the store.
- **NFR-004** — summing 1 000 entries matches the arithmetic sum exactly.
- **Uniqueness** — FR-002, FR-004, FR-007 and FR-010 are each a uniqueness rule, and EC-9 says two
  managers acting at the same instant must not both win. Only the database can settle that.
- **NFR-005** — one organisation, ~100 people, 200 projects, ~50 000 entries a year. Small.
- The repository is a `dotnet new webapp` template with no test project and no infrastructure. The
  cost of *starting* matters as much as the cost of running.

## Decision

Persist through **Entity Framework Core 10 on SQLite**, with real migrations checked into
`Data/Migrations/`.

- One `TimeRegistrationDbContext` holding the whole slice.
- The database is a file (`timeregistration.db`) named by the `TimeRegistration` connection string.
- `db.Database.MigrateAsync()` runs at startup; development additionally seeds `DemoData`.
- Every uniqueness rule is a **unique index over a persisted normalized column**
  (`NormalizedName`, `NormalizedEmail`, uppercased with `ToUpperInvariant()`), not SQLite's `NOCASE`
  collation — `NOCASE` folds ASCII only, and EC-7 is explicitly about Turkish dotted/dotless *i*.
- Services check for a duplicate first to produce a friendly message, and also catch
  `DbUpdateException` with SQLite error 19 and translate it back into the *same* message. The query
  is for the user; the index is for correctness.

## Consequences

**Good**

- Zero setup: no server to install, no container, no connection string to hand out. `dotnet run`
  works on a clean checkout.
- Tests run against the real engine. `ProjectSetupFixture` opens
  `Data Source=:memory:` and holds the connection open — the unique indexes that carry FR-002/004/
  007/010 are actually exercised. The EF InMemory provider would have enforced none of them.
- LINQ-to-SQL translation gives us the single aggregate query NFR-001 asks for; `SuccessCriteria`
  asserts a command count of exactly 1 for a 200-project, 50 000-entry list.
- Migrating to PostgreSQL or SQL Server later is a provider swap plus a regenerated migration, not a
  rewrite, as long as nothing leans on SQLite-specific SQL. Nothing currently does.

**Bad / accepted**

- One writer at a time. Fine at NFR-005's scale; it is the first thing to bite if this ever becomes
  multi-tenant.
- SQLite has no `decimal` type and EF cannot translate `SUM` over a decimal to it reliably. This is
  the direct cause of [ADR-0003](0003-durations-as-whole-minutes.md) — durations are whole minutes,
  so the aggregate is an integer `SUM` the store can do itself.
- Dynamic typing means the schema is weaker than the model. The unique indexes still hold, which is
  what the spec depends on.
- A file database is not a deployment story. `docs/architecture/07-deployment-view.md` is still
  empty, and this ADR does not fill it in.

## Alternatives considered

| Option | Why not |
| --- | --- |
| **In-memory collections** | Nothing survives a restart, and EC-9's two-managers-at-once race has no arbiter. Would have to be replaced before the first real user. |
| **EF Core InMemory provider** | Not a database. Enforces no unique index, so every uniqueness rule in the spec would be untested and unenforced. |
| **PostgreSQL / SQL Server** | Correct at scale and the likely destination, but demands a server, a container or a connection string before anyone can run the app. Premature for one organisation of 100 people, and the escape route above is cheap. |
| **Dapper or raw ADO.NET** | Would leave the change tracking, migrations and LINQ translation to hand-written code. The slice has no query hot enough to justify it. |
