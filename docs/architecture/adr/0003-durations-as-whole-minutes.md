# ADR-0003: Durations are whole minutes, not decimal hours

| | |
| --- | --- |
| **Status** | Accepted |
| **Date** | 2026-09-15 |
| **Deciders** | Stijn |
| **Context** | [Spec 001](../../specs/001-project-setup.md) §5.1, §5.3, NFR-004 · [Spec 005](../../specs/005-weekly-time-grid.md) §5.3, §10 Q2 |

## Context

Spec 001 originally modelled hours as a decimal with two places, and NFR-004 requires that summing
1 000 entries match the arithmetic sum exactly. Spec 005 §5.3 then introduced a `Duration` value
object backed by whole minutes and explicitly amended spec 001 to match, with §10 Q2 noting the
change had to land "before build starts". This ADR records why, and closes that question.

Two decimal places do not divide an hour cleanly. Twenty minutes is 0.333… hours; stored as `0.33`
it loses 0.4 minutes, and three of them lose a minute and a bit. People notice when a day of 20-minute
entries does not add up to what they typed.

There is an implementation reason too. SQLite has no decimal type, and EF Core will not translate a
`SUM` over a decimal to it — the aggregate would have to happen in the web process, which is exactly
what [ADR-0002](0002-burned-hours-derived-never-stored.md) and NFR-001 forbid.

## Decision

**A duration is an `int` count of whole minutes**, wrapped in a `Duration` value struct
(`Domain/Duration.cs`).

- `Project.BudgetMinutes` and `TimeEntry.DurationMinutes` are `int` columns.
- `Duration.Parse` accepts both `hh:mm` ("120:30") and a decimal number of hours ("120", "7.5"),
  always in the invariant culture so a budget means the same thing on every server.
- A value that does not land on a whole minute is **refused, not rounded** (spec 001 EC-3):
  `120.333` hours is 7 219.98 minutes, and the user is asked for `120:20` instead. Rounding is the
  thing this decision exists to avoid, so the code never does it quietly.
- `Duration.ToHhMm(int)` renders a *signed* count, because remaining hours go negative (EC-13) and
  are never clamped.
- `MaxMinutes` is 100 000 hours — the fat-finger guard from EC-4, not a business limit.

Spec 001 §5.1 and §5.3 have been amended accordingly (`budgetHours` → `budgetMinutes`), and spec 005
§10 Q2 is resolved.

## Consequences

**Good**

- NFR-004 holds by construction rather than by discipline: integer addition has no rounding rule to
  get wrong, and `SuccessCriteria` demonstrates it over 1 000 awkward entry lengths.
- The burned-hours aggregate is an integer `SUM` SQLite performs itself, which is what makes
  ADR-0002 affordable.
- One type owns parsing, the cap, and the hh:mm rendering, so the same rules apply to a project
  budget and to a time entry without either restating them.

**Bad / accepted**

- Minutes are the floor. Anything finer than a minute cannot be recorded — acceptable for
  timesheets, and no requirement asks for it.
- `int` caps out around 4 000 years of minutes; `MaxMinutes` sits far below that, so overflow is not
  reachable.
- Users who think in decimal hours must be met halfway. `Duration.Parse` accepts `7.5`, and the
  create/edit forms take the budget as free text rather than a number input, so a rejected value can
  be handed back verbatim instead of being silently coerced.

## Alternatives considered

| Option | Why not |
| --- | --- |
| **`decimal(5,2)` hours, as spec 001 first had it** | 20 minutes is not representable; SQLite cannot sum it server-side; spec 005 amended it away. |
| **`TimeSpan`** | Carries sub-minute precision we do not want, and EF maps it to a string or ticks — neither sums well in SQLite. |
| **`double` hours** | Floating-point accumulation is what NFR-004 names as unacceptable. |
| **Store minutes but expose decimal hours in the API/UI** | Reintroduces the rounding at the boundary; the hh:mm rendering is clearer anyway. |
