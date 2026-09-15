# ADR-0002: Burned hours are derived on every read, never stored

| | |
| --- | --- |
| **Status** | Accepted |
| **Date** | 2026-09-15 |
| **Deciders** | Stijn |
| **Context** | [Spec 001 — Project Setup](../../specs/001-project-setup.md) §5.4, §9.3, NFR-003 |

## Context

The project list shows three numbers per project: budget, hours burned, hours remaining (FR-013).
Only the budget is a fact somebody entered. The other two are consequences:

```
burned    = SUM(duration of every time entry on the project)
remaining = budget - burned
```

The obvious optimisation is to keep a running `BurnedMinutes` column on `Project` and adjust it when
an entry is written. That column can then be wrong — and a budget figure nobody trusts is worse than
no figure at all. Spec 001 anticipates this and makes it NFR-003: *"computed from live data on each
read. No cached or precomputed totals, so the figures can never disagree with the underlying
entries."*

Several rules only make sense if the total is derived:

- **FR-014** — every entry counts regardless of its timesheet week's status. A cached total would
  need invalidating on approve, reject, unsubmit and edit as well as on write.
- **EC-14** — hours logged by someone since unassigned still count. Unassigning must not touch a
  total.
- **SC-011 / EC-13** — lowering a budget below what is burned is legal, and the overrun is shown in
  full. Remaining is signed, never clamped.
- **FR-017 / SC-016** — a project with no entries burns `0:00`, not a blank.

Spec 010 reinforces it: "Burned hours ignore status. Spec 001 FR-014 holds unchanged."

## Decision

**Burned and remaining hours are computed inside the query that reads the projects, and are not
stored anywhere.**

`ProjectService.ListAsync` projects the aggregate into the same statement that reads the rows:

```csharp
.Select(p => new {
    p.Id, p.Name, ClientName = p.Client!.Name, p.BudgetMinutes,
    BurnedMinutes = p.TimeEntries.Sum(e => (int?)e.DurationMinutes) ?? 0,
    AssignedPeopleCount = p.Assignments.Count(),
})
```

- The `(int?)` cast is what makes an empty project read `0` instead of blank — FR-017.
- `TimeEntry` carries a non-unique index on `ProjectId` so the aggregate is an index scan.
- `BudgetPosition(BudgetMinutes, BurnedMinutes)` is a value struct that derives `RemainingMinutes`,
  `Utilisation` and `Status` (within / approaching at ≥ 90% / over). It holds no stored state either,
  so the badge and the number can never contradict each other.
- The same projection serves the detail page, plus a correlated subquery for what each assigned
  person logged — the figure the FR-011 unassign warning quotes.

## Consequences

**Good**

- The figures cannot drift. There is no second copy to go stale, so no invalidation to get wrong on
  entry edits, week approvals, unassignment or budget changes.
- NFR-001 is met by shape, not by caching: one statement for the whole page. `SuccessCriteria`
  asserts a command count of exactly 1 against 200 projects and 50 000 entries, well under the
  500 ms budget.
- Spec 005 and spec 010 can write, edit and delete entries without knowing this page exists.

**Bad / accepted**

- Every page view re-aggregates. At NFR-005's scale (~50 000 entries a year) this is an index scan
  SQLite finishes in single-digit milliseconds. It will not stay free forever.
- If the data ever outgrows that, the answer is a materialised view or a summary table maintained by
  the store — **not** a hand-maintained column on `Project`. Superseding this ADR should be a
  deliberate act with a measurement attached.

## Alternatives considered

| Option | Why not |
| --- | --- |
| **`Project.BurnedMinutes` maintained on write** | Needs correct invalidation from every write path in specs 005 and 010, including status changes that FR-014 says must *not* change the total. One missed path and the number is silently wrong forever. |
| **Cache the list with a short TTL** | Directly contradicts NFR-003, and buys nothing at this scale — the query is already the cheap part. |
| **One aggregate query per project** | Correct, but N+1: 201 statements for the list page. Explicitly ruled out by NFR-001. |
| **Compute in memory after loading entries** | Would pull 50 000 rows into the web process to produce 200 numbers. |
